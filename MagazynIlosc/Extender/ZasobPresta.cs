using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Start.Presta.MagazynIlosc.Extender
{
    public class ZasobyPresta
    {
        public ZasobyPresta()
        {

        }

        public List<ZasobPresta> zasobyPresta {get; set;}
    }

    public class ZasobPresta
    {
        public ZasobPresta()
        {
                
        }

        public string Id { get; set; }
        public string IdTowar { get; set; }

        public string Ilosc { get; set; }
    }
}
