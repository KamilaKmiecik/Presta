using Soneta.Business;
using Soneta.Business.UI;
using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Xml.Serialization;
using Soneta.Towary;
using System.Collections.Generic;
using System.IO;
using static Google.Apis.Requests.BatchRequest;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using Soneta.Produkcja;
using Mono.CSharp;
using static Soneta.Business.Query;
using Start.Presta.ZakladanieTowaru.Extender;
using Soneta.Core.Schedule;
using System.Data.SqlTypes;
using System.Xml.Schema;

[assembly: Worker(typeof(ZakladanieTowaruPrestaWorker), typeof(Towary))]

namespace Start.Presta.ZakladanieTowaru.Extender
{
    public class ZakladanieTowaruPrestaWorker
    {
        [Context]
        public Session session { get; set; }

        [Context]
        public Towar[] Towary { get; set; }


        public HttpClient client = new HttpClient();


        [Action("Presta/Załóż produkt wg filtowania", Mode = ActionMode.SingleSession | ActionMode.Progress)]
        public void DodajZeSchematu()
        {
            client.AuthorizeClientBasic("NHNWVYTFUJQIX8I2HP4HEZC9CTAD8XCC");
            ServicePointManager.ServerCertificateValidationCallback = (request, cert, chain, errors) => true;

            var endpoint = "https://slezinski.pl/presta/api/products";

            var schema = client.GetAsync("https://slezinski.pl/presta/api/products?schema=blank").GetAwaiter().GetResult();
            schema.EnsureSuccessStatusCode();

            string xmlSchema = schema.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var schemaDocument = XDocument.Parse(xmlSchema);
            XElement produkt = schemaDocument.Descendants("product").FirstOrDefault();
            produkt.CleanUpSchema(schemaDocument); 


            var tm = TowaryModule.GetInstance(session);
            var cenaPodstawowa = tm.DefinicjeCen.WgNazwy["Podstawowa"];

            var config = new ConfigExtender() { Session = session }; 

            foreach (Towar towar in Towary)
            {
                var status = (bool)towar.Features["Status"];
                var metaopis = (string)towar.Features["MetaOpis"]; 
                var cenaString = towar.Ceny[cenaPodstawowa].Netto.Value.ToString();

                produkt.ChangeFieldValue("price", cenaString);
                produkt.ChangeFieldValue("state", (status ? "1" : "0"));
                produkt.ChangeFieldValue("unit_price", cenaString);
                produkt.ChangeFieldValue("active", (config.Dostepny ? "1" : "0"));
                produkt.ChangeFieldValue("meta_description", "language", metaopis, 2);
                produkt.ChangeFieldValue("name", "language", towar.Nazwa);
                produkt.ChangeFieldValue("name", "language", towar.Nazwa, 2);
                //umm -.-

                var content = new StringContent(schemaDocument.ToString(), Encoding.UTF8, "application/xml");

                var response = client.PostAsync(endpoint, content).GetAwaiter().GetResult();
                //response.IsSuccessStatusCode
                string xmlData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var xmlDocument = XDocument.Parse(xmlData);

                XElement produktPoDodaniu = xmlDocument.Descendants("product").FirstOrDefault();
                produktPoDodaniu.CleanUpSchema(xmlDocument);

                using (ITransaction transaction = session.Logout(true))
                {
                    //int idTowar = GetLastID();
                    towar.Features["IdPresta"] = produktPoDodaniu.GetFieldValue("id");
                    transaction.CommitUI();
                }
            }
        }

        private static string GetBase64StringForImage(string imagePath)
        {
            byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);
            return Convert.ToBase64String(imageBytes);
        }

        //Kilka osób doda i wpisze złe
        //private int GetLastID()
        //{
        //    var ostatnieIdRequest = "https://slezinski.pl/presta/api/products?sort=[id_DESC]&limit=1&display=[id]";
        //    var response = client.GetStringAsync(ostatnieIdRequest).GetAwaiter().GetResult();

        //    XmlDocument xmlDocument = new XmlDocument();
        //    xmlDocument.LoadXml(response);

        //    XmlNode idNode = xmlDocument.SelectSingleNode("/prestashop/products/product/id");

        //    string idValue = idNode.InnerText;

        //    return Convert.ToInt32(idValue);
        //}

        //[Action("Presta/Załóż produkt ze schematu", Mode = ActionMode.SingleSession | ActionMode.Progress)]
        public void Dodaj()
        {
            client.AuthorizeClientBasic("NHNWVYTFUJQIX8I2HP4HEZC9CTAD8XCC");
            ServicePointManager.ServerCertificateValidationCallback = (request, cert, chain, errors) => true;

            var endpoint = "https://slezinski.pl/presta/api/products";

            var schema = client.GetAsync("https://slezinski.pl/presta/api/products?schema=blank").GetAwaiter().GetResult();
            schema.EnsureSuccessStatusCode();

            string xmlSchema = schema.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var schemaDocument = XDocument.Parse(xmlSchema);
            XElement produkt = schemaDocument.Descendants("product").FirstOrDefault();
            produkt.CleanUpSchema(schemaDocument);


            produkt.ChangeFieldValue("price", "66"); 
            ////ChangeFieldValue("price", "66", produkt);
            //ChangeFieldValue("state", "1", produkt);
            //ChangeFieldValue("wholesale_price", "157", produkt);
            //ChangeFieldValue("unit_price", "66", produkt);
            //ChangeFieldValue("active", "1", produkt);
            //ChangeFieldValue("available_for_order", "1", produkt);
            //ChangeFieldValue("meta_description", "language", "babo", 1, produkt);
            //ChangeFieldValue("meta_description", "language", "glupia", 2, produkt);
            //ChangeFieldValue("name", "language", "miss", 1, produkt);
            //ChangeFieldValue("name", "language", "maam", 2, produkt);

            var content = new StringContent(schemaDocument.ToString(), Encoding.UTF8, "application/xml");

            var response = client.PostAsync(endpoint, content).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            string xmlData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var xmlDocument = XDocument.Parse(xmlData);

        }

        //[Action("Presta/Załóż produkt", Mode = ActionMode.SingleSession | ActionMode.Progress)]
        public void Dodawanie()
        {
            //Bez sensu :) czytać dokumentaję
            client.AuthorizeClientBasic("NHNWVYTFUJQIX8I2HP4HEZC9CTAD8XCC");
            //gregory
            ServicePointManager.ServerCertificateValidationCallback = (request, cert, chain, errors) => true;

            var prestashop = new prestashop();

            var productName = new prestashopProductLanguage
            {
                id = 1,
                Value = "Nazwa"
            };

            var product = new prestashopProduct
            {
                id = "28",
                price = "35.900000",
                //name = new prestashopProductLanguage[] { productName }
            };

            prestashop.products = new prestashopProduct[] { product };

            var serializer = new XmlSerializer(typeof(prestashop));

            var xmlSettings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = false
            };

            var xml = new StringBuilder();
            using (var writer = XmlWriter.Create(xml, xmlSettings))
            {
                serializer.Serialize(writer, prestashop);
            }

            var endpoint = "https://slezinski.pl/presta/api/products";
            var content = new StringContent(xml.ToString(), Encoding.UTF8, "application/xml");

            var response = client.PostAsync(endpoint, content).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            //using (ITransaction transaction = SessionGlobal.Logout(true))
            //{

            //    var towar = new Towar();
            //    SessionGlobal.GetTowary().Towary.AddRow(towar);
            //    towar.Nazwa = @params.Nazwa;
            //    towar.EAN = @params.EAN;

            //    transaction.CommitUI();

            //}
            //SessionGlobal.Save();

            //Pusty schemat
            //var schema = client.GetStringAsync("https://slezinski.pl/presta/api/products?schema=blank").GetAwaiter().GetResult();

            //var produktyPresta = new List<prestashopProduct>(); 
            // var prestaShop = new prestashop(); 
            // foreach (Towar item in Towary)
            // {
            //     var produktPresta = new prestashopProduct(26);
            //     //produktPresta.id = 26;
            //     //produktPresta.name.SetValue(item.Nazwa, 0);
            //     //produktPresta.name.SetValue(item.Nazwa, 1);
            //     //prestashopProductLanguage[] nazwa;

            //     //produktyPresta.Add(produktPresta);
            //     prestaShop.products[0] = produktPresta; 
            // }

            // var serializer = new XmlSerializer(typeof(prestashop));

            // var writer = new StringWriter(); 
            // using (writer)
            // {
            //     serializer.Serialize(writer, prestaShop); 
            // }
            // var w = writer.ToString(); 
            // var content = new StringContent(writer.ToString(), Encoding.UTF8, "application/xml");

            //// var content = new FormUrlEncodedContent(produktyPresta);
            // var response = client.PostAsync("https://slezinski.pl/presta/api/products", content).GetAwaiter().GetResult();
            // response.EnsureSuccessStatusCode(); 
        }

    }
}
