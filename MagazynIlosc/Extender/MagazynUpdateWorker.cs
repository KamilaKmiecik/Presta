using Soneta.Business;
using Soneta.Business.App;
using Soneta.Business.UI;
using Soneta.CRM;
using Soneta.Handel;
using Soneta.Magazyny;
using Soneta.Towary;
using Start.Presta.MagazynIlosc.Extender;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Xml.Linq;

[assembly: Worker(typeof(MagazynUpdateWorker), typeof(Soneta.Magazyny.Zasoby))]

namespace Start.Presta.MagazynIlosc.Extender
{
    public class MagazynUpdateWorker
    {
        [Context]
        public Login Login { get; set; }

        public HttpClient client = new HttpClient();

        [Action("Presta/Zasoby", Mode = ActionMode.SingleSession | ActionMode.Progress)]
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

                    var towar = session.GetTowary().Towary.WgNazwy.FirstOrDefault(x => string.Equals(x.Features["IdPresta"], towarId));

                    var okresMagazynowy = session.GetHandel().Magazyny.OkresyMag.WgOkres.FirstOrDefault();
                    
                    var magazyn = session.GetHandel().Magazyny.Magazyny.WgNazwa["Firma"];

                    using (ITransaction transaction = session.Logout(true))
                    {
                        var zas = new Zasob(okresMagazynowy, magazyn, towar);
                        session.AddRow(zas);
                        //zas.PartiaTowaru
                        var partiaTowaru = new PartiaTowaru();
                        partiaTowaru.Typ = TypPartii.Brak;
                        partiaTowaru.KontrahentPartii = session.GetCRM().Kontrahenci.WgNazwy.FirstOrDefault();  
                        //session.AddRow(partiaTowaru); 

                        //zas.PartiaTowaru = new PartiaTowaru(); 
                        transaction.CommitUI(); 
                    }
                    session.Save();
                }
            }
        }
    }
}
