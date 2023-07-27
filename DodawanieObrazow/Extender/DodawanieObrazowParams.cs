using Soneta.Business;
using Soneta.Business.UI;
using Soneta.Types;
using Start.Presta.DodawanieObrazow.Extender;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Worker(typeof(DodawanieObrazowParams))]
namespace Start.Presta.DodawanieObrazow.Extender
{
    public class DodawanieObrazowParams : ContextBase
    {
        public DodawanieObrazowParams(Context context) : base(context)
        {
            //musi być jakiś typ sonetowski
           Date data = new Date();
        }

        public string AltOpis { get; set; }
        public string Opis { get; set; }


        public string Plik { get; set; } = "*.jpg";

        public object GetListPlik()
        {
            return new FileDialogInfo
            {
                Title = "Wybierz plik"
            };
        }
    }
}
