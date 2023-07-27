
// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
using Mono.CSharp;
using static Syncfusion.XlsIO.Parser.Biff_Records.Charts.ChartSeriesRecord;
using System.Xml.Serialization;
/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "", ElementName = "prestashop" , IsNullable = false)]
public partial class prestashopC
{

    private prestashopCustomer[] customersField;

    public prestashopC()
    {
            
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlArrayItemAttribute("customer", IsNullable = false)]
    public prestashopCustomer[] customers
    {
        get
        {
            return this.customersField;
        }
        set
        {
            this.customersField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class prestashopCustomer
{
    public prestashopCustomer()
    {
            
    }

    private string idField;

    private string lastnameField;

    private string firstnameField;

    /// <remarks/>
    public string id
    {
        get
        {
            return this.idField;
        }
        set
        {
            this.idField = value;
        }
    }

    /// <remarks/>
    public string lastname
    {
        get
        {
            return this.lastnameField;
        }
        set
        {
            this.lastnameField = value;
        }
    }

    /// <remarks/>
    public string firstname
    {
        get
        {
            return this.firstnameField;
        }
        set
        {
            this.firstnameField = value;
        }
    }
}

