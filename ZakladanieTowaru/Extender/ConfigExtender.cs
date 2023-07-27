using Soneta.Business;
using Soneta.Config;
using Start.Config.Extender;
using Start.Presta.ZakladanieTowaru.Extender;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Worker(typeof(ConfigExtender))]

namespace Start.Presta.ZakladanieTowaru.Extender
{
    public class ConfigExtender
    {
        [Context]
        public Session Session { get; set; }

        public bool Aktywny
        {
            get { return GetValue("Aktywny", false); }
            set { SetValue("Aktywny", value, AttributeType._boolean); }
        }

        public bool Dostepny
        {
            get { return GetValue("Dostepny", false); }
            set { SetValue("Dostepny", value, AttributeType._boolean); }
        }

        //POMOCNICZE
        private void SetValue<T>(string name, T value, AttributeType type)
        {
            SetValue(Session, name, value, type);
        }

        //Metoda odpowiada za ustawianie wartosci parametrów konfiguracji
        public static void SetValue<T>(Session session, string name, T value, AttributeType type)
        {
            using (var t = session.Logout(true))
            {
                var cfgManager = new CfgManager(session);
                //wyszukiwanie gałęzi głównej 
                var node1 = cfgManager.Root.FindSubNode("Start", false) ??
                            cfgManager.Root.AddNode("Start", CfgNodeType.Node);

                //wyszukiwanie liścia 
                var node2 = node1.FindSubNode("Towary ceny nazwy", false) ??
                            node1.AddNode("Towary ceny nazwy", CfgNodeType.Leaf);

                //wyszukiwanie wartosci atrybutu w liściu 
                var attr = node2.FindAttribute(name, false);
                if (attr == null)
                    node2.AddAttribute(name, type, value);
                else
                    attr.Value = value;

                t.CommitUI();
            }
        }

        //Metoda odpowiada za pobieranie wartosci parametrów konfiguracji
        private T GetValue<T>(string name, T def)
        {
            return GetValue(Session, name, def);
        }

        //Metoda odpowiada za pobieranie wartosci parametrów konfiguracji
        public static T GetValue<T>(Session session, string name, T def)
        {
            var cfgManager = new CfgManager(session);

            var node1 = cfgManager.Root.FindSubNode("Start", false);

            //Jeśli nie znaleziono gałęzi, zwracamy wartosć domyślną
            if (node1 == null) return def;

            var node2 = node1.FindSubNode("Towary ceny nazwy", false);
            if (node2 == null) return def;

            var attr = node2.FindAttribute(name, false);
            if (attr == null) return def;

            if (attr.Value == null) return def;

            return (T)attr.Value;
        }
    }
}
