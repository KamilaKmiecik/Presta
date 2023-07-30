using System.Xml.Serialization;
using System.IO;
using System;
using System.IdentityModel.Tokens.Jwt;
using Soneta.Towary;
using Start.Presta;
using Soneta.CRM;
using Soneta.Handel;
using Soneta.Business;
using Start.ZakladanieTowaru.Extender;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using Soneta.Types.Extensions;
using System.Xml;
using System.Linq;
using Start.Presta.ZakladanieTowaru.Extender;
using Soneta.Business.UI;
using System.Text;
using Soneta.Types;
using System.Globalization;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Collections.Generic;
using System.Xml.Linq;
using static Soneta.Core.ComparePodmiot;

[assembly: Worker(typeof(PrestaStringWorker), typeof(Towary))]
public class PrestaStringWorker
{
    [Context]
    public Session session { get; set; }

    public HttpClient client = new HttpClient(); 

    [Action("Presta/Deserializuj", Mode = ActionMode.SingleSession | ActionMode.Progress)]
    public object Deserializacja()
    {
        client.AuthorizeClientBasic();

        //gregory
        ServicePointManager.ServerCertificateValidationCallback = (request, cert, chain, errors) => true;
        var response = client.GetStringAsync("https://slezinski.pl/presta/api/products?display=[id, name, state, price]").GetAwaiter().GetResult();
        //var response = client.GetStringAsync("https://slezinski.pl/presta/api/products").GetAwaiter().GetResult();
        XmlSerializer serializer = new XmlSerializer(typeof(prestashop));
        var prestaProdukty = new prestashop();

        using (TextReader reader = new StringReader(response))
        {
            prestaProdukty = (prestashop)new XmlSerializer(typeof(prestashop)).Deserialize(reader);
        }

        var produkty = new StringBuilder();
        foreach (prestashopProduct item in prestaProdukty.products)
        {
            var view = session.GetTowary().Towary.CreateView();
            view.Condition &= new FieldCondition.Equal("Features.IdPresta", item.id); 
            var towary = view.ToArray<Towar>();
            if (towary.Any())
                continue;

            var kombinacje = GetKombinacje(item.id); 

            var tm = TowaryModule.GetInstance(session);
            var cenaPodstawowa = tm.DefinicjeCen.WgNazwy["Podstawowa"];

            var ceny = tm.DefinicjeCen.WgNazwy.ToList();

            using (ITransaction transaction = session.Logout(true))
            {
                if (kombinacje == null)
                {
                    var towar = new Towar();
                    session.GetTowary().Towary.AddRow(towar);
                    towar.Features["IdPresta"] = item.id;
                    towar.Features["Status"] = string.Equals(item.state, "1");


                    if (Decimal.TryParse(item.price, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal result))
                        towar.Ceny[cenaPodstawowa].Netto = new DoubleCy(result); ;

                    if (!string.IsNullOrEmpty(item.name[1].Value) || kombinacje == null)
                        towar.Nazwa = item.name[1].Value;
                    else
                        towar.Nazwa = "Domyslna nazwa";

                    produkty.Append(item.id + "\t" + towar.Nazwa + "\n");
                    transaction.CommitUI();
                }
                else
                {
                    foreach (var kombinacja in kombinacje)
                    {
                        var towar = new Towar();
                        session.GetTowary().Towary.AddRow(towar);
                        towar.Features["IdPresta"] = item.id;
                        towar.Features["Status"] = string.Equals(item.state, "1");

                        if (kombinacje != null)
                        {
                            towar.Features["IdKombinacja"] = kombinacja.Id.ToString();
                            towar.Features["IdOpcja"] = kombinacja.nazwa;

                            towar.Nazwa = item.name[1].Value + kombinacja.nazwa;
                        }

                        if (Decimal.TryParse(item.price, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal result))
                            towar.Ceny[cenaPodstawowa].Netto = new DoubleCy(result); 

                        produkty.Append(item.id + "\t" + towar.Nazwa + "\n");
                        transaction.CommitUI();

                    }
                }
                

            }

        }
        return new MessageBoxInformation("Wynik", $"Importowano dane {produkty}");

    }

    public List<(int Id, string nazwa)> GetKombinacje(string productId)
    {
        var request = "https://slezinski.pl/presta/api/combinations?filter[id_product]="+ productId +"&display=full";
        var response = client.GetStringAsync(request).GetAwaiter().GetResult(); 

        List<(int Id, List<int> ProductOptionValueIds)> pairs = new List<(int Id, List<int> ProductOptionValueIds)>();
        List<(int Id, string nazwa)> nazwy = new List<(int Id, string nazwa)>();
        XDocument xDocument = XDocument.Parse(response);

        var combinationsNode = xDocument.Element("prestashop")?.Element("combinations");
        if (combinationsNode == null || !combinationsNode.Elements("combination").Any())
            return null; // No combinations found, return null to handle the empty case

        var requestOptions = "https://slezinski.pl/presta/api/product_option_values?display=full";
        var responseOptions = client.GetStringAsync(requestOptions).GetAwaiter().GetResult();
        XDocument optionsXml = XDocument.Parse(responseOptions);
        var options = optionsXml.Descendants("product_option_value");

        var combinationNodes = xDocument.Descendants("combination");

        foreach (var combinationNode in combinationNodes)
        {
            int id = int.Parse(combinationNode.Element("id").Value);
            var productOptionValueNodes = combinationNode.Descendants("product_option_values");

            List<int> productOptionValueIds = new List<int>();
            string nazwaKombinacja = ""; 
            foreach (var productOptionValueNode in productOptionValueNodes)
            {
                nazwaKombinacja += " " + options.FirstOrDefault(e => e.Element("id")?.Value == productOptionValueNode.Value).GetFieldValue("name", "language"); ;

                productOptionValueIds.Add(int.Parse(productOptionValueNode.Value));
            }

            nazwy.Add((id, nazwaKombinacja));
            pairs.Add((id, productOptionValueIds));
        }

        return nazwy;
    }

    public object GetId()
    {
        client.AuthorizeClientBasic();

        //gregory
        ServicePointManager.ServerCertificateValidationCallback = (request, cert, chain, errors) => true;
        var response = client.GetStringAsync("https://slezinski.pl/presta/api/products?display=[name, id]").GetAwaiter().GetResult();
        //var response = client.GetStringAsync("https://slezinski.pl/presta/api/products").GetAwaiter().GetResult();
        XmlSerializer serializer = new XmlSerializer(typeof(prestashop));
        var prestaProdukty = new prestashop();

        using (TextReader reader = new StringReader(response))
        {
            prestaProdukty = (prestashop)new XmlSerializer(typeof(prestashop)).Deserialize(reader);
        }

        var produkty = new StringBuilder();
        foreach (prestashopProduct item in prestaProdukty.products)
        {
            var view = session.GetTowary().Towary.CreateView();
            view.Condition &= new FieldCondition.Equal("Features.IdPresta", item.id);
            //view.Condition |= new FieldCondition.Equal("Nazwa", item.name[1].Value);
            var towary = view.ToArray<Towar>();
            if (towary.Any())
                continue;

            using (ITransaction transaction = session.Logout(true))
            {
                var towar = new Towar();
                session.GetTowary().Towary.AddRow(towar);


                towar.Features["IdPresta"] = item.id;
                if (!string.IsNullOrEmpty(item.name[1].Value))
                    towar.Nazwa = item.name[1].Value;
                else
                    towar.Nazwa = "Domyslna nazwa";

                produkty.Append(item.id + "\t" + towar.Nazwa + "\n");
                transaction.CommitUI();
            }

        }
        return new MessageBoxInformation("Wynik", $"Importowano dane {produkty}");

    }
}