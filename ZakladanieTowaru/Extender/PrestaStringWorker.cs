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
            //view.Condition |= new FieldCondition.Equal("Nazwa", item.name[1].Value);
            var towary = view.ToArray<Towar>();
            if (towary.Any())
                continue;

            var tm = TowaryModule.GetInstance(session);
            var cenaPodstawowa = tm.DefinicjeCen.WgNazwy["Podstawowa"];

            using (ITransaction transaction = session.Logout(true))
            {
                var towar = new Towar();
                session.GetTowary().Towary.AddRow(towar);
                towar.Features["IdPresta"] = item.id;
                towar.Features["Status"] = string.Equals(item.state, "1");

                decimal result;
                if(Decimal.TryParse(item.price, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result));
                    towar.Ceny[cenaPodstawowa].Netto = new DoubleCy(result); 

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