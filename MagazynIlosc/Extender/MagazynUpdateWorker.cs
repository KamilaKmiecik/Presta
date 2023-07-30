using Mono.CSharp;
using Org.BouncyCastle.Asn1.Ocsp;
using Soneta.Business;
using Soneta.Business.App;
using Soneta.Business.UI;
using Soneta.CRM;
using Soneta.Handel;
using Soneta.Magazyny;
using Soneta.Tools;
using Soneta.Towary;
using Soneta.Types;
using Start.Presta.MagazynIlosc.Extender;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;

[assembly: Worker(typeof(MagazynUpdateWorker), typeof(Soneta.Magazyny.Zasoby))]

namespace Start.Presta.MagazynIlosc.Extender
{
    public class MagazynUpdateWorker
    {
        [Context]
        public Login Login { get; set; }

        public HttpClient client = new HttpClient();

        [Context]
        public Session session { get; set; }

        [Context]
        public Soneta.Magazyny.Zasob[] Zasoby { get; set; }

        [Action("Presta/Zasoby z presty", Mode = ActionMode.SingleSession | ActionMode.Progress)]
        public void Zamowienia()
        {
            client.AuthorizeClientBasic();
            ServicePointManager.ServerCertificateValidationCallback = (request, cert, chain, errors) => true;

            var requestAll = "https://slezinski.pl/presta/api/stock_availables?display=full";
            var response = client.GetStringAsync(requestAll).GetAwaiter().GetResult();

            // Zasob([Required] OkresMagazynowy okres, [Required] Magazyn magazyn, [Required] Towar towar
            XDocument xmlZasoby = XDocument.Parse(response);
            List<XElement> zasoby = xmlZasoby.Descendants("stock_available").ToList();
            //List<XElement> zasobyLista = new List<XElement>();

            foreach (var zasob in zasoby)
            {
                using (Session session = Login.CreateSession(false, false, "Sesja"))
                {
                    var zasobId = zasob.GetFieldValue("id");

                    var stzasoby = new SubTable(session.GetHandel().Magazyny.Zasoby.WgMagazyn);
                    stzasoby = stzasoby[new FieldCondition.Equal("Features.IdPrestaZasoby", zasobId)];
                    var zasobArray = stzasoby.ToArray<Zasob>();

                    if (zasobArray.Any())
                        continue;

                    var towarId = zasob.GetFieldValue("id_product");
                    var ilosc = zasob.GetFieldValue("quantity");

                    var tm = TowaryModule.GetInstance(session);
                    var cenaPodstawowa = tm.DefinicjeCen.WgNazwy["Podstawowa"];

                    var towar = session.GetTowary().Towary.WgNazwy.FirstOrDefault(x => string.Equals(x.Features["IdPresta"], towarId));
                    var okresMagazynowy = session.GetHandel().Magazyny.OkresyMag.WgOkres.FirstOrDefault();
                    var magazyn = session.GetHandel().Magazyny.Magazyny.WgNazwa["Firma"];

                    using (ITransaction transaction = session.Logout(true))
                    {
                        var dokHandlowy = new DokumentHandlowy();
                        dokHandlowy.Definicja = session.GetHandel().DefDokHandlowych.WgSymbolu["PW"];
                        dokHandlowy.Magazyn = session.GetMagazyny().Magazyny[1];
                        session.AddRow(dokHandlowy);

                        dokHandlowy.Data = Date.Now;

                        var pozycja = new PozycjaDokHandlowego(dokHandlowy);
                        session.AddRow(pozycja);
                        pozycja.Towar = towar;
                        var iloscDouble = double.TryParse(ilosc, out double value) ? value : 0;
                        pozycja.Ilosc = new Quantity(iloscDouble);
                        pozycja.WartoscCy = towar.Ceny[cenaPodstawowa].Netto * iloscDouble;
                        transaction.CommitUI();
                    }
                    session.Save();
                }
            }
        }

        [Action("Presta/Zasoby z presty zz", Mode = ActionMode.SingleSession | ActionMode.Progress)]
        public void ZamowieniaZKombinacjami()
        {
            client.AuthorizeClientBasic();
            ServicePointManager.ServerCertificateValidationCallback = (request, cert, chain, errors) => true;

            var requestAll = "https://slezinski.pl/presta/api/stock_availables?display=full";
            var response = client.GetStringAsync(requestAll).GetAwaiter().GetResult();

            // Zasob([Required] OkresMagazynowy okres, [Required] Magazyn magazyn, [Required] Towar towar
            XDocument xmlZasoby = XDocument.Parse(response);
            List<XElement> zasoby = xmlZasoby.Descendants("stock_available").ToList();
            //List<XElement> zasobyLista = new List<XElement>();

            foreach (var zasob in zasoby)
            {
                using (Session session = Login.CreateSession(false, false, "Sesja"))
                {
                    var zasobId = zasob.GetFieldValue("id");

                    var stzasoby = new SubTable(session.GetHandel().Magazyny.Zasoby.WgMagazyn);
                    stzasoby = stzasoby[new FieldCondition.Equal("Features.IdPrestaZasoby", zasobId)];
                    var zasobArray = stzasoby.ToArray<Zasob>();

                    if (zasobArray.Any())
                        continue;

                    var towarId = zasob.GetFieldValue("id_product");
                    var ilosc = zasob.GetFieldValue("quantity");

                    var tm = TowaryModule.GetInstance(session);
                    var cenaPodstawowa = tm.DefinicjeCen.WgNazwy["Podstawowa"];

                    var towar = session.GetTowary().Towary.WgNazwy.FirstOrDefault(x => string.Equals(x.Features["IdPresta"], towarId));
                    var okresMagazynowy = session.GetHandel().Magazyny.OkresyMag.WgOkres.FirstOrDefault();
                    var magazyn = session.GetHandel().Magazyny.Magazyny.WgNazwa["Firma"];

                    using (ITransaction transaction = session.Logout(true))
                    {
                        var dokHandlowy = new DokumentHandlowy();
                        dokHandlowy.Definicja = session.GetHandel().DefDokHandlowych.WgSymbolu["PW"];
                        dokHandlowy.Magazyn = session.GetMagazyny().Magazyny[1];
                        session.AddRow(dokHandlowy);

                        dokHandlowy.Data = Date.Now;

                        var pozycja = new PozycjaDokHandlowego(dokHandlowy);
                        session.AddRow(pozycja);
                        pozycja.Towar = towar;
                        var iloscDouble = double.TryParse(ilosc, out double value) ? value : 0;
                        pozycja.Ilosc = new Quantity(iloscDouble);
                        pozycja.WartoscCy = towar.Ceny[cenaPodstawowa].Netto * iloscDouble;
                        transaction.CommitUI();
                    }
                    session.Save();
                }
            }
        }

        [Action("Presta/Zasoby do presty bez", Mode = ActionMode.SingleSession | ActionMode.Progress)]
        public void ZamowieniaPresta()
        {
            client.AuthorizeClientBasic();
            ServicePointManager.ServerCertificateValidationCallback = (request, cert, chain, errors) => true;

            var requestAll = "https://slezinski.pl/presta/api/stock_availables?display=full";
            var response = client.GetStringAsync(requestAll).GetAwaiter().GetResult();

            XDocument xmlZasoby = XDocument.Parse(response);
            List<XElement> zasoby = xmlZasoby.Descendants("stock_available").ToList();

            var zasobyLista = new List<ZasobPresta>();
            var zasobyPresta = new ZasobyPresta();

            foreach (Soneta.Magazyny.Zasob zasob in Zasoby)
            {
                var produktId = zasob.Towar.Features["IdPresta"];
                var zasobPresta = zasoby.FirstOrDefault(z => string.Equals(z.GetFieldValue("id_product"), produktId));

                if (zasobPresta == null)
                    continue;

                var iloscPresta = double.TryParse(zasobPresta.GetFieldValue("quantity"), out double value) ? value : 0;
                var ilosc = zasob.Ilosc.Value + iloscPresta;
                zasobPresta.ChangeFieldValue("quantity", ilosc.ToString());

            }

            XDocument zasobyBody = new XDocument(new XElement("prestashop"));
            XElement rootElement = zasobyBody.Root;
            XNamespace xlinkNamespace = "http://www.w3.org/1999/xlink";
            rootElement.Add(new XAttribute(XNamespace.Xmlns + "xlink", xlinkNamespace));
            rootElement.Add(zasoby);

            //var request = client.PutAsync("https://slezinski.pl/presta/api/stock_availables", zasoby.ToString()).GetAwaiter().GetResult();
            var content = new StringContent(zasobyBody.ToString(), Encoding.UTF8, "application/xml");

            var responsePut = client.PutAsync("https://slezinski.pl/presta/api/stock_availables", content).GetAwaiter().GetResult();
            responsePut.EnsureSuccessStatusCode();
        }

        [Action("Presta/Zasoby do presty zz", Mode = ActionMode.SingleSession | ActionMode.Progress)]
        public void ZamowieniaPrestaRozroxnisne()
        {
            client.AuthorizeClientBasic();
            ServicePointManager.ServerCertificateValidationCallback = (request, cert, chain, errors) => true;

            var requestAll = "https://slezinski.pl/presta/api/stock_availables?display=full";
            var response = client.GetStringAsync(requestAll).GetAwaiter().GetResult();

            XDocument xmlZasoby = XDocument.Parse(response);
            List<XElement> zasoby = xmlZasoby.Descendants("stock_available").ToList();

            var zasobyLista = new List<ZasobPresta>();
            var zasobyPresta = new ZasobyPresta();

            foreach (Soneta.Magazyny.Zasob zasob in Zasoby)
            {
                var produktId = zasob.Towar.Features["IdPresta"];
                var kombinacjaId = zasob.Towar.Features["IdKombinacja"];
                var zasobPresta = zasoby.FirstOrDefault(z => string.Equals(z.GetFieldValue("id_product"), produktId) && string.Equals(z.GetFieldValue("id_product_attribute"), kombinacjaId));

                if (zasobPresta == null)
                    continue;

                var iloscPresta = double.TryParse(zasobPresta.GetFieldValue("quantity"), out double value) ? value : 0;
                var ilosc = zasob.Ilosc.Value + iloscPresta;
                zasobPresta.ChangeFieldValue("quantity", ilosc.ToString());

            }

            XDocument zasobyBody = new XDocument(new XElement("prestashop"));
            XElement rootElement = zasobyBody.Root;
            XNamespace xlinkNamespace = "http://www.w3.org/1999/xlink";
            rootElement.Add(new XAttribute(XNamespace.Xmlns + "xlink", xlinkNamespace));
            rootElement.Add(zasoby);

            //var request = client.PutAsync("https://slezinski.pl/presta/api/stock_availables", zasoby.ToString()).GetAwaiter().GetResult();
            var content = new StringContent(zasobyBody.ToString(), Encoding.UTF8, "application/xml");

            var responsePut = client.PutAsync("https://slezinski.pl/presta/api/stock_availables", content).GetAwaiter().GetResult();
            responsePut.EnsureSuccessStatusCode();
        }
    }
}


