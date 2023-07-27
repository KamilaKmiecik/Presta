using Mono.CSharp;
using Soneta.Business;
using Soneta.Business.App;
using Soneta.Business.UI;
using Soneta.CRM;
using Soneta.CRM.UI;
using Soneta.Handel;
using Soneta.Towary;
using Soneta.Types;
using Start.Presta.Zamowienia.Extender;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;


[assembly: Worker(typeof(ZamowieniaDoEnovyWorker), typeof(DokumentHandlowy))]

namespace Start.Presta.Zamowienia.Extender
{
    public class ZamowieniaDoEnovyWorker
    {
        [Context]
        public Login Login { get; set; }

        public HttpClient client = new HttpClient();

        [Action("Presta/Dokumenty handlowe", Mode = ActionMode.SingleSession | ActionMode.Progress)]
        public void Zamowienia()
        {
            client.AuthorizeClientBasic();
            ServicePointManager.ServerCertificateValidationCallback = (request, cert, chain, errors) => true;

            var zamowienieRequest = "https://slezinski.pl/presta/api/orders?display=full";
            var response = client.GetStringAsync(zamowienieRequest).GetAwaiter().GetResult();

            XDocument xmlDoc = XDocument.Parse(response);
            IEnumerable<XElement> orders = xmlDoc.Descendants("order");
            List<XElement> orderElements = new List<XElement>();

            foreach (XElement item in orders)
            {
                orderElements.Add(item);
            }

            foreach (var order in orderElements)
            {
                using (Session session = Login.CreateSession(false, false, "Sesja"))
                {
                    var orderId = order.GetFieldValue("id");
                    var view = session.GetHandel().DokHandlowe.CreateView();
                    view.Condition &= new FieldCondition.Equal("Features.IdPrestaDokHan", orderId);
                    var dokumentV = view.ToArray<DokumentHandlowy>();
                    if (dokumentV.Any())
                        continue;

                    var customerId = order.GetFieldValue("id_customer");
                    var addressId = order.GetFieldValue("id_address_delivery");

                    var associations = order.Descendants("associations").FirstOrDefault();
                    var produkty = GetProductPairs(associations.ToString());

                    var config = new ZamowieniaDoEnovyConfig() { Session = session };

                    using (ITransaction tr = session.Logout(true))
                    {
                        var dokument = new DokumentHandlowy();
                        dokument.Definicja = session.GetHandel().DefDokHandlowych.WgSymbolu[config.SymbolDokumentuHandlowego];
                        dokument.Magazyn = session.Get(config.Magazyn);
                        session.AddRow(dokument);

                        var kontrahent = GetKontrahent(customerId, addressId, session);
                        dokument.Kontrahent = kontrahent;
                        dokument.OdbiorcaMiejsceDostawy = GetOdbiorcaMiejsceDostawy(session, addressId, kontrahent);
                        tr.CommitUI();
                        var dataDokument = DateTime.TryParse(order.GetFieldValue("date_add"), out DateTime data) ? data : DateTime.Now;
                        dokument.Data = new Date(dataDokument);

                        dokument.Features["IdPrestaDokHan"] = orderId;

                        foreach (var item in produkty)
                        {
                            var pozycja = new PozycjaDokHandlowego(dokument);
                            session.AddRow(pozycja);
                            pozycja.Towar = session.GetTowary().Towary.WgNazwy.Where(t => string.Equals(t.Features["IdPresta"], item.Item1)).FirstOrDefault();

                            var ilosc = double.TryParse(item.Item2, out double value) ? value : 0;
                            pozycja.Ilosc = new Quantity(ilosc);

                            tr.CommitUI();
                        }
                    }
                    session.Save();
                }
            }
        }

        private List<Tuple<string, string>> GetProductPairs(string response)
        {
            XDocument xmlDoc = XDocument.Parse(response);
            IEnumerable<XElement> orderRows = xmlDoc.Descendants("order_row");
            List<XElement> orderRowElements = new List<XElement>();

            foreach (XElement orderRow in orderRows)
            {
                orderRowElements.Add(orderRow);
            }

            List<Tuple<string, string>> pairs = new List<Tuple<string, string>>();

            foreach (var row in orderRowElements)
            {
                var idProduct = row.GetFieldValue("product_id");
                var quantity = row.GetFieldValue("product_quantity");

                pairs.Add(new Tuple<string, string>(idProduct, quantity));
            }

            return pairs;
        }


        //Z reguly po prostu do ulicy razem z numerem 
        private (string, string) UlicaNr(string ulicaNr)
        {
            int commaIndex = ulicaNr.IndexOf(',');
            string nr = "0", ulica = "0";

            if (commaIndex <= 0)
            {
                int spaceIndex = ulicaNr.IndexOf(' ');
                if (spaceIndex != -1)
                {
                    nr = ulicaNr.Substring(0, spaceIndex).Trim();
                    ulica = ulicaNr.Substring(spaceIndex + 1).Trim();

                }
                else
                {
                    ulica = ulicaNr;
                }
            }
            else
            {
                nr = ulicaNr.Substring(0, commaIndex).Trim();
                ulica = ulicaNr.Substring(commaIndex + 1).Trim();
            }

            return (ulica, nr);
        }

        //Srednio potrzebne
        private Lokalizacja GetOdbiorcaMiejsceDostawy(Session session, string addressId, Kontrahent kontrahent)
        {
            var requestIdAddress = "https://slezinski.pl/presta/api/addresses/" + addressId;

            var responseId = client.GetStringAsync(requestIdAddress).GetAwaiter().GetResult();

            XDocument addressDocument = XDocument.Parse(responseId);
            var address = addressDocument.Descendants("address").FirstOrDefault();
            var miasto = address.GetFieldValue("city");

            string ulicaNr = address.GetFieldValue("address1");

            string nr = UlicaNr(ulicaNr).Item2, ulica = UlicaNr(ulicaNr).Item1;

            var countryId = address.GetFieldValue("id_country");
            var requestIdCountry = "https://slezinski.pl/presta/api/countries/" + countryId;
            var responseCountry = client.GetStringAsync(requestIdCountry).GetAwaiter().GetResult();

            XDocument xmlDocKraj = XDocument.Parse(responseCountry);

            XElement kraj = xmlDocKraj.Descendants("language").FirstOrDefault(e => (string)e.Attribute("id") == "2");

            var nazwa = miasto + " " + ulica;
            var st = new SubTable(session.GetCRM().Lokalizacje.WgKontrahent);
            st = st[new FieldCondition.Equal("Nazwa", nazwa)];
            var lokalizacje = st.ToArray<Lokalizacja>();

            if (!lokalizacje.Any())
            {
                var lokalizacja = new Lokalizacja();

                session.GetCRM().Lokalizacje.AddRow(lokalizacja);
                lokalizacja.Nazwa = nazwa;
                lokalizacja.Kontrahent = kontrahent;
                lokalizacja.Adres.Kraj = kraj.Value;
                lokalizacja.Adres.Miejscowosc = miasto;
                lokalizacja.Adres.Ulica = ulica;
                lokalizacja.Adres.NrDomu = nr;
                return lokalizacja;
            }
            return null;
        }


        // ulica. kod-pocztowy?, miejscowosc, kraj
        private Kontrahent GetKontrahent(string customerId, string addressId, Session session)
        {
            var requestIdName = "https://slezinski.pl/presta/api/customers/" + customerId;

            var response = client.GetStringAsync(requestIdName).GetAwaiter().GetResult();

            XDocument customerDocument = XDocument.Parse(response);
            var customer = customerDocument.Descendants("customer").FirstOrDefault();

            var customerFirstName = customer.GetFieldValue("firstname");
            var customerLastName = customer.GetFieldValue("lastname");

            var requestIdAddress = "https://slezinski.pl/presta/api/addresses/" + addressId;

            var responseId = client.GetStringAsync(requestIdAddress).GetAwaiter().GetResult();

            XDocument addressDocument = XDocument.Parse(responseId);
            var address = addressDocument.Descendants("address").FirstOrDefault();
            var miasto = address.GetFieldValue("city");

            string ulicaNr = address.GetFieldValue("address1");

            string nr = UlicaNr(ulicaNr).Item2, ulica = UlicaNr(ulicaNr).Item1;

            string countryId = address.GetFieldValue("id_country");
            var requestIdCountry = "https://slezinski.pl/presta/api/countries/" + countryId;
            var responseCountry = client.GetStringAsync(requestIdCountry).GetAwaiter().GetResult();

            XDocument xmlDoc = XDocument.Parse(responseCountry);

            XElement kraj = xmlDoc.Descendants("language").FirstOrDefault(e => (string)e.Attribute("id") == "2");

            //var st = new SubTable(SessionGlobal.GetCRM().Kontrahenci.WgKodu);
            //st = st[new FieldCondition.Equal("Features.IdPrestaKontr", customerId)];
            //var kkk = st.ToArray<Kontrahent>();
            var view = session.GetCRM().Kontrahenci.CreateView();

            view.Condition &= new FieldCondition.Equal("Features.IdPrestaKontr", customerId);
            var kontrahent = view.ToArray<Kontrahent>();

            if (kontrahent.Any())
                return kontrahent.FirstOrDefault(x => string.Equals(x.Features["IdPrestaKontr"], customerId)); ;


            var kontrahentPresta = new Kontrahent();

            using (ITransaction tr = session.Logout(true))
            {
                session.GetCRM().Kontrahenci.AddRow(kontrahentPresta);

                kontrahentPresta.Kod = customerFirstName + customerLastName + customerId;
                kontrahentPresta.Nazwa = customerFirstName + " " + customerLastName;
                kontrahentPresta.Features["IdPrestaKontr"] = customerId;
                kontrahentPresta.Adres.Kraj = kraj.Value;
                kontrahentPresta.Adres.Miejscowosc = miasto;
                kontrahentPresta.Adres.Ulica = ulica;
                kontrahentPresta.Adres.NrDomu = nr;

                tr.CommitUI();
            }

            return kontrahentPresta;

        }

    }

}
