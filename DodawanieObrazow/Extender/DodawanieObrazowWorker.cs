using iTextSharp.text;
using Soneta.Business;
using Soneta.Business.UI;
using Soneta.Towary;
using Start.Presta.DodawanieObrazow.Extender;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

[assembly: Worker(typeof(DodawanieObrazowWorker), typeof(Towary))]

namespace Start.Presta.DodawanieObrazow.Extender
{
    public class DodawanieObrazowWorker
    {
        [Context]
        public DodawanieObrazowParams @params { get; set; }

        [Context]
        public Session session { get; set; }

        [Context]
        public Towar[] Towary { get; set; }

        public HttpClient client = new HttpClient();

        private const string baseUrl = "https://slezinski.pl/presta";
        //private const int produktId = 113;

        [Action("Presta/Dodaj obraz do towaru", Mode = ActionMode.SingleSession | ActionMode.Progress)]
        public object DodawanieObrazow()
        {
            foreach (Towar item in Towary)
            {
                client.AuthorizeClientBasic();

                var produktId = (string)item.Features["IdPresta"];
                string path = @params.Plik; 
                string imageAltTag = @params.AltOpis;
                string imageDescription = @params.Opis;

                var obrazId = ZapiszObraz(path, imageAltTag, imageDescription, produktId);

                //obrocic ifa? 
                if (!string.IsNullOrEmpty(obrazId) && !string.IsNullOrEmpty(produktId) && (path.EndsWith(".jpg")  || path.EndsWith(".jpeg")) )
                {
                    DodajObrazDoProduktu(obrazId, produktId);
                    return new MessageBoxInformation("Sukces", "Dodano obraz");
                }
                else
                {
                    return new MessageBoxInformation("Blad", "Nie dodano obrazu lub obraz nie jest w formacie .jpg");
                }
            }

            return 0; 
        }

        private string ZapiszObraz(string path, string altTag, string description, string produktId)
        {

            client.AuthorizeClientBasic();
            
            string apiUrl = $"{baseUrl}/api/images/products/{produktId}";

            try
            {
                var formData = new MultipartFormDataContent();
                    
                byte[] imageData = File.ReadAllBytes(path);

                formData.Add(new ByteArrayContent(imageData), "image", Path.GetFileName(path));
                formData.Add(new StringContent("products"), "type");
                formData.Add(new StringContent(produktId.ToString()), "id_product");

                var response = client.PostAsync(apiUrl, formData).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var imageId = jsonResponse;
                    return imageId;
                }
                 
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return "";
        }

        private void DodajObrazDoProduktu(string imageId, string produktId)
        {
            client.AuthorizeClientBasic();
            string apiUrl = $"{baseUrl}/api/products/{produktId}";
            
            try
            {
                string jsonData = $"{{\"product\":{{\"id\":{produktId},\"id_default_image\":{imageId}}}}}";
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                var response = client.PutAsync(apiUrl, content).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

            }
            catch (Exception ex)
            {
                //throw new Exception(ex.Message);
            }
        }

    }

}
