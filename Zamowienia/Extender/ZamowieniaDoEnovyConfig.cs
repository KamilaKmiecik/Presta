using Soneta.Business;
using Soneta.Config;
using Soneta.Handel;
using Soneta.Magazyny;
using Soneta.Towary;
using Start.DokumentyHandlowe.Extender;
using Start.Presta.Zamowienia.Extender;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Soneta.Business.FieldValue;
using System;

[assembly: Worker(typeof(ZamowieniaDoEnovyConfig))]

namespace Start.Presta.Zamowienia.Extender
{
    public class ZamowieniaDoEnovyConfig
    {
        [Context]
        public Session Session { get; set; }

        public string SymbolDokumentuHandlowego
        {
            get
            {
                return GetValue("SymbolDokumentuHandlowego", "");
            }
            set
            {
                SetValue("SymbolDokumentuHandlowego", value, AttributeType._string);
            }
        }

        public object GetListSymbolDokumentuHandlowego() => Session.GetHandel().DefDokHandlowych.WgSymbolu.Select(x => x.Symbol).ToArray();

        public Magazyn Magazyn
        {
            get
            {
                var guid = GetValue("Magazyn", System.Guid.Empty);
                return guid == System.Guid.Empty ? null : Session.GetHandel().Magazyny.Magazyny[guid];
            }
            set { SetValue("Magazyn", value?.Guid ?? System.Guid.Empty, AttributeType._guid); }
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
                var node1 = cfgManager.Root.FindSubNode("Presta", false) ??
                            cfgManager.Root.AddNode("Presta", CfgNodeType.Node);

                //wyszukiwanie liścia 
                var node2 = node1.FindSubNode("Presta", false) ??
                            node1.AddNode("Presta", CfgNodeType.Leaf);

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

            var node1 = cfgManager.Root.FindSubNode("Presta", false);

            //Jeśli nie znaleziono gałęzi, zwracamy wartosć domyślną
            if (node1 == null) return def;

            var node2 = node1.FindSubNode("Presta", false);
            if (node2 == null) return def;

            var attr = node2.FindAttribute(name, false);
            if (attr == null) return def;

            if (attr.Value == null) return def;

            return (T)attr.Value;
        }
    }
}