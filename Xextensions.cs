using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Net;

namespace Start.Presta
{
    public static class Xextensions
    {
        public static void ChangeFieldValue(this XElement element, string fieldName, string value)
        {
            if (!string.IsNullOrEmpty(value))
                element.Elements().Where(x => x.Name == fieldName).First().ReplaceWith(new XElement(fieldName, new XCData(value)));
        }

        public static string GetFieldValue(this XElement element, string fieldName)
        {
            if (!string.IsNullOrEmpty(fieldName))
                return element.Elements().Where(x => x.Name == fieldName).First().Value;
            else
                return string.Empty; 
        }

        public static void ChangeFieldValue(this XElement element, string fieldName, string latterFieldName, string fieldValue)
        {
            if (!string.IsNullOrEmpty(fieldValue))
                element.Elements().Where(x => x.Name == fieldName).FirstOrDefault().Elements().Where(x => x.Name == latterFieldName).FirstOrDefault().ReplaceWith(new XElement(latterFieldName, new XCData(fieldValue)));
        }

        public static string GetFieldValue(this XElement element, string fieldName, string latterFieldName)
        {
            if (!string.IsNullOrEmpty(fieldName))
               return element.Elements().Where(x => x.Name == fieldName).FirstOrDefault().Elements().Where(x => x.Name == latterFieldName).FirstOrDefault().Value;
            else
                return string.Empty;
        }

        public static void AddFieldValue(this XElement element, string fieldName, string latterFieldName, string fieldValue)
        {
            if (!string.IsNullOrEmpty(fieldValue))
                element.Elements().Where(x => x.Name == fieldName).FirstOrDefault().Elements().Where(x => x.Name == latterFieldName).FirstOrDefault().ReplaceWith(new XElement(latterFieldName, new XCData(fieldValue)));
        }


        public static void ChangeFieldValue(this XElement element, string fieldName, string latterFieldName, string fieldValue, int id)
        {
            if (!string.IsNullOrEmpty(fieldValue))
                element.Elements().Where(x => x.Name == fieldName).FirstOrDefault().Elements().Where(x => x.Name == latterFieldName).ElementAt(id - 1).ReplaceWith(new XElement(latterFieldName, new XAttribute("id", id), new XCData(fieldValue)));
        }


        public static XElement CleanUpSchema(this XElement element, XDocument schemaDocument)
        {
            List<string> elements = new List<string>
            {
                "product_type", "type", "redirect_type", "id_type_redirected",
                "id_manufacturer", "manufacturer_name", "quantity", "quantity_discount",
                "minimal_quantity", "id_default_image", "id_default_combination",
                "position_in_category", "manufacturer_name"
            };

            foreach (var item in elements)
            {
                element.Elements().Where(x => x.Name == item).Remove();
            }

            return element;
        }

        public static HttpClient AuthorizeClientBasic(this HttpClient client, string tokenString = "NHNWVYTFUJQIX8I2HP4HEZC9CTAD8XCC")
        {
            tokenString += ":";
            var tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenString);
            var token = System.Convert.ToBase64String(tokenBytes);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", "Basic " + token);
            ServicePointManager.ServerCertificateValidationCallback = (request, cert, chain, errors) => true;
            return client;
        }
    }
}
