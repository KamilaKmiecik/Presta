using Soneta.Business;
using Soneta.Business.UI;
using Soneta.CRM;
using Soneta.Towary;
using Start.DaneDodatkowychTabel.Punktacja;
using Start.Presta.ZakladanieTowaru.Extender;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;

[assembly: Worker(typeof(EdytowanieTowaruPrestaWorker), typeof(Towary))]

namespace Start.Presta.ZakladanieTowaru.Extender
{
    public class EdytowanieTowaruPrestaWorker
    {
        [Context]
        public Session session { get; set; }

        [Context]
        public Towar[] Towary { get; set; }

        public HttpClient client = new HttpClient();


        [Action("Presta/Zmien status", Mode = ActionMode.SingleSession | ActionMode.Progress)]
        public void UstawStatus()
        {
            client.AuthorizeClientBasic();
            ServicePointManager.ServerCertificateValidationCallback = (request, cert, chain, errors) => true;

            foreach (Towar towar in Towary)
            {
                string id = (string)towar.Features["IdPresta"];
                var productRequest = "https://slezinski.pl/presta/api/products/" + id;

                var retrievedProduct = client.GetStringAsync(productRequest).GetAwaiter().GetResult();

                var productDocument = XDocument.Parse(retrievedProduct);
                XElement product = productDocument.Descendants("product").FirstOrDefault();
                product.CleanUpSchema(productDocument);


                var status = (bool)towar.Features["Status"];

                //bo zmienia status
                product.ChangeFieldValue("state", (status ? "0" : "1"));
                var content = new StringContent(productDocument.ToString(), Encoding.UTF8, "application/xml");


                var response = client.PutAsync(productRequest, content).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();


                if(response.IsSuccessStatusCode)
                    using (ITransaction transaction = session.Logout(true))
                    {
                        towar.Features["Status"] = !(bool)towar.Features["Status"];
                        transaction.CommitUI();
                    }
            }
        }

        [Action("Presta/Ustaw status na aktywny", Mode = ActionMode.SingleSession | ActionMode.Progress)]
        public void UstawStatusNaAktywny()
        {
            client.AuthorizeClientBasic();
            ServicePointManager.ServerCertificateValidationCallback = (request, cert, chain, errors) => true;

            foreach (Towar towar in Towary)
            {
                string id = (string)towar.Features["IdPresta"];
                var productRequest = "https://slezinski.pl/presta/api/products/" + id;

                var retrievedProduct = client.GetStringAsync(productRequest).GetAwaiter().GetResult();

                var productDocument = XDocument.Parse(retrievedProduct);
                XElement product = productDocument.Descendants("product").FirstOrDefault();
                product.CleanUpSchema(productDocument);


                var status = (bool)towar.Features["Status"];

                product.ChangeFieldValue("state", "1");
                var content = new StringContent(productDocument.ToString(), Encoding.UTF8, "application/xml");


                var response = client.PutAsync(productRequest, content).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();


                if (response.IsSuccessStatusCode)
                    using (ITransaction transaction = session.Logout(true))
                    {
                        towar.Features["Status"] = !(bool)towar.Features["Status"];
                        transaction.CommitUI();
                    }
            }
        }
    }
}
