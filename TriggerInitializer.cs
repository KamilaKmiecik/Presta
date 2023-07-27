using Soneta.Business;
using Soneta.CRM;
using Soneta.Handel;
using Soneta.Towary;
using Start.DaneDodatkowychTabel;
using Start.DaneDodatkowychTabel.Punktacja;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

[assembly: ProgramInitializer(typeof(Start.Presta.TriggerInitializer))]
namespace Start.Presta
{
    public class TriggerInitializer : IProgramInitializer
    {
        void IProgramInitializer.Initialize()
        {
            var triggers = new Triggery();
        }
    }


    public class Triggery
    {
        //MUSI BYĆ STATYCZNA bo pamięć
        static Triggery()
        {
            //TowaryModule.TowarSchema.AddNazwaAfterEdit(new RowDelegate<TowaryModule.TowarRow>(Dodawanie)); 
        }


        //Spk, tylko Ci wywali jak nie będzie internetu/padne presta -- lepiej automat 
        private static void Dodawanie(TowaryModule.TowarRow towar)
        {
            if (!string.IsNullOrEmpty((string)towar.Features["IdPresta"]))
            {
                HttpClient client = new HttpClient();
                //string token = "NHNWVYTFUJQIX8I2HP4HEZC9CTAD8XCC"; 
                client.AuthorizeClientBasic();
                ServicePointManager.ServerCertificateValidationCallback = (request, cert, chain, errors) => true;

                var productRequest = "https://slezinski.pl/presta/api/products/" + (string)towar.Features["IdPresta"];

                var retrievedProduct = client.GetStringAsync(productRequest).GetAwaiter().GetResult();

                var productDocument = XDocument.Parse(retrievedProduct);
                XElement product = productDocument.Descendants("product").FirstOrDefault();
                product.CleanUpSchema(productDocument);

                //bo zmienia status
                product.ChangeFieldValue("name", "language", towar.Nazwa);
                product.ChangeFieldValue("name", "language", towar.Nazwa, 2);
                var content = new StringContent(productDocument.ToString(), Encoding.UTF8, "application/xml");


                var response = client.PutAsync(productRequest, content).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
            }
        }
    }
}

