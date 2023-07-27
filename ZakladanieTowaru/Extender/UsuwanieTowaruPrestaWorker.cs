using Soneta.Business;
using Soneta.Business.UI;
using Soneta.Towary;
using Start.Presta.ZakladanieTowaru.Extender;
using System;
using System.Net.Http;
using System.Net;
using System.Text;

[assembly: Worker(typeof(UsuwanieTowaruPrestaWorker), typeof(Towary))]

namespace Start.Presta.ZakladanieTowaru.Extender
{
    public class UsuwanieTowaruPrestaWorker
    {
        [Context]
        public Session session { get; set; }

        [Context]
        public Towar[] Towary { get; set; }


        public HttpClient client = new HttpClient();


        [Action("Presta/Usuń towar", Mode = ActionMode.SingleSession | ActionMode.Progress)]
        public object UsunTowar()
        {
            client.AuthorizeClientBasic();
            ServicePointManager.ServerCertificateValidationCallback = (request, cert, chain, errors) => true;

            string id = "";
            var endpoint = "https://slezinski.pl/presta/api/products";
            var usunieteProdukty = new StringBuilder();

            foreach (Towar towar in Towary)
            {
                using (ITransaction transaction = session.Logout(true))
                {
                    id = (string)towar.Features["IdPresta"];

                    if (!string.IsNullOrEmpty(id))
                    {
                        endpoint = "https://slezinski.pl/presta/api/products/" + id;
                        var deleteRequest = client.DeleteAsync(endpoint);
                    }
                    else
                        continue;

                    usunieteProdukty.Append((string)towar.Features["IdPresta"] + "\t" + towar.Nazwa + "\n");
                    towar.Delete();
                    transaction.CommitUI();
                }
            }

            return new MessageBoxInformation("Wynik", $"Usunieto pozycje:\n {usunieteProdukty}");
        }
    }
}
