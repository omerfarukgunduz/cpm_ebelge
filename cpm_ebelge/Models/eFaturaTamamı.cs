namespace cpm_ebelge.Models
{
    public class eFaturaProtectedValues
    {
        public string doc { get; set; }
        public bool resend { get; set; }
        public BaseUbl createdUBL { get; set; } // VAR TİPİNDEYDİ NE OLDUGUNU BİLMEDĞİMİZ İÇİN DYNAMIC TANIMLADIK
        public dynamic strFatura { get; set; } // VAR TİPİNDEYDİ NE OLDUGUNU BİLMEDĞİMİZ İÇİN DYNAMIC TANIMLADIK
    }

    public abstract class EFatura : EIrsaliye_EFatura
    {
        protected eFaturaProtectedValues eFaturaProtectedValues { get; set; } = new eFaturaProtectedValues();
        ///GONDER----------------------------------------------------------------------------------------------------
        public override string Gonder(int EVRAKSN)
        {
            DateTime dt = DateTime.Now;
            eFaturaProtectedValues.doc = GeneralCreator.GetUBLInvoiceData(EVRAKSN);
            eFaturaProtectedValues.resend = false;
            if (doc != null)
            {
                Connector.m.PkEtiketi = doc.PK;
                eFaturaProtectedValues.createdUBL = doc.BaseUBL;  // Fatura UBL i oluşturulur

                if (createdUBL.ProfileID.Value == "IHRACAT")
                    Connector.m.PkEtiketi = "urn:mail:ihracatpk@gtb.gov.tr";
                else
                    Connector.m.PkEtiketi = doc.PK;

                if (doc.OLDGUID == "")
                    Entegrasyon.EfagdnUuid(EVRAKSN, createdUBL.UUID.Value);
                else
                    eFaturaProtectedValues.resend = true;

                if (UrlModel.SelectedItem == "DPLANET")
                    eFaturaProtectedValues.createdUBL.ID = eFaturaProtectedValues.createdUBL.ID ?? new UblInvoiceObject.IDType { Value = "CPM" + DateTime.Now.Year + EVRAKSN.ToString("000000000") };

                if (UrlModel.SelectedItem == "QEF" && Connector.m.SablonTip)
                {
                    var sablon = string.IsNullOrEmpty(doc.ENTSABLON) ? Connector.m.Sablon : doc.ENTSABLON;

                    if (eFaturaProtectedValues.createdUBL.Note == null)
                        eFaturaProtectedValues.createdUBL.Note = new UblInvoiceObject.NoteType[0];

                    var list = createdUBL.Note.ToList(); //var list idi
                    list.Add(new UblInvoiceObject.NoteType { Value = $"#EFN_SERINO_TERCIHI#{sablon}#" }); ;
                    createdUBL.Note = list.ToArray();
                }
                //createdUBL.ID = createdUBL.ID ?? new UblInvoiceObject.IDType { Value = "GIB2022000000001 " };

                InvoiceSerializer serializer = new InvoiceSerializer(); // UBL  XML e dönüştürülür
                if (Connector.m.SchematronKontrol)
                {
                    schematronResult = SchematronChecker.Check(createdUBL, SchematronDocType.eFatura);
                    if (schematronResult.SchemaResult != "Başarılı" || schematronResult.SchematronResult != "Başarılı")
                        throw new Exception(schematronResult.Detail);
                }
                eFaturaProtectedValues.strFatura = serializer.GetXmlAsString(createdUBL); // XML byte tipinden string tipine dönüştürülür

                var Result = new Results.EFAGDN();
            }

        }
        public sealed class FIT_ING_INGBANKEFatura : EFatura
        {
            
        }
        public sealed class DPlanetEFatura : EFatura
        {
           
        }
        public sealed class EDMEFatura : EFatura
        {
           
        }
        public sealed class QEFEFatura : EFatura
        {
           
        }
        ///------------------------------------------------------------------------------------------------------------------
        ///TOPLUGONDER--------------------------------------------------------------------------------------------------------
        public override string TopluGonder(List<BaseInvoiceUBL> Faturalar, string ENTSABLON)
        {
            StringBuilder sb = new StringBuilder();
            UBLBaseSerializer serializer = new InvoiceSerializer(); // UBL  XML e dönüştürülür

            var Result = new Results.EFAGDN();

            List<InvoiceType> Faturalar2 = new List<InvoiceType>();

            foreach (var Fatura in Faturalar)
            {
                int EVRAKSN = Convert.ToInt32(Fatura.BaseUBL.AdditionalDocumentReference.FirstOrDefault(elm => elm.DocumentTypeCode.Value == "CUST_INV_ID")?.ID.Value ?? "0");

                if (UrlModel.SelectedItem == "DPLANET")
                    Fatura.BaseUBL.ID = Fatura.BaseUBL.ID ?? new UblInvoiceObject.IDType { Value = "CPM" + DateTime.Now.Year + EVRAKSN.ToString("000000000") };

                if (UrlModel.SelectedItem == "QEF" && Connector.m.SablonTip && Fatura?.BaseUBL != null && Fatura?.BaseUBL?.ID?.Value == "CPM" + DateTime.Now.Year + "000000001")
                {
                    var sablon = string.IsNullOrEmpty(Fatura.ENTSABLON) ? Connector.m.Sablon : Fatura.ENTSABLON;
                    var list = Fatura?.BaseUBL.Note.ToList();
                    list.Add(new UblInvoiceObject.NoteType { Value = $"#EFN_SERINO_TERCIHI#{sablon}#" });
                    Fatura.BaseUBL.Note = list.ToArray();
                }
                //Fatura.BaseUBL.ID = Fatura.BaseUBL.ID ?? new UblInvoiceObject.IDType { Value = "GIB2022000000001 " };

                Faturalar2.Add(Fatura.BaseUBL);
            }

            if (Connector.m.SchematronKontrol)
            {
                InvoiceSerializer ser = new InvoiceSerializer();
                foreach (var Fatura in Faturalar2)
                {
                    eFaturaProtectedValues.schematronResult = SchematronChecker.Check(Fatura, SchematronDocType.eFatura);
                    if (schematronResult.SchemaResult != "Başarılı" || schematronResult.SchematronResult != "Başarılı")
                        throw new Exception(schematronResult.Detail);
                }
            }

        }
        public sealed class FIT_ING_INGBANKEFatura : EFatura
        {
        }
        public sealed class DPlanetEFatura : EFatura
        {
        }
        public sealed class EDMEFatura : EFatura
        {
        }
        public sealed class QEFEFatura : EFatura
        {

        }

        ///------------------------------------------------------------------------------------------------------------------
        ///-ESLE-------------------------------------------------------------------------------------------------------
        public override void Esle(List<int> Value, bool DontShow = false)
        {
        }
        public sealed class FIT_ING_INGBANKEFatura : EFatura
        {
        }
        public sealed class DPlanetEFatura : EFatura
        {
        }
        public sealed class EDMEFatura : EFatura
        {
        }
        public sealed class QEFEFatura : EFatura
        {
        }

        ///------------------------------------------------------------------------------------------------------------------
        ///--ALINAN FATURALAR LISTESİ-----------------------------------------------------------------------------------------------
        public override List<AlinanBelge> AlinanFaturalarListesi(DateTime StartDate, DateTime EndDate)
        {

        }
        public sealed class FIT_ING_INGBANKEFatura : EFatura
        {
        }
        public sealed class DPlanetEFatura : EFatura
        {
        }
        // İÇİ BOŞTU.
        public sealed class EDMEFatura : EFatura
        {
        }
        public sealed class QEFEFatura : EFatura
        {
        }

        ///------------------------------------------------------------------------------------------------------------------
        ///-İNDİR-----------------------------------------------------------------------------------------------------------
        public override void Indir(DateTime day1, DateTime day2)
        {

        }
        public sealed class FIT_ING_INGBANKEFatura : EFatura
        {
        }
        public sealed class DPlanetEFatura : EFatura
        {
        }
        public sealed class EDMEFatura : EFatura
        {
        }
        public sealed class QEFEFatura : EFatura
        {
        }

        ///------------------------------------------------------------------------------------------------------------------
        ///-KABUL-----------------------------------------------------------------------------------------------------------
        public override void Kabul(string GUID, string Aciklama)
        {

        }
        public sealed class FIT_ING_INGBANKEFatura : EFatura
        {
        }
        public sealed class DPlanetEFatura : EFatura
        {
        }
        public sealed class EDMEFatura : EFatura
        {
        }
        public sealed class QEFEFatura : EFatura
        {
        }

        ///------------------------------------------------------------------------------------------------------------------
        ///-RED--------------------------------------------------------------------------------------------------------------
        public override void Red(string GUID, string Aciklama)
        {

        }
        public sealed class FIT_ING_INGBANKEFatura : EFatura
        {



        }
        public sealed class DPlanetEFatura : EFatura
        {
        }
        public sealed class EDMEFatura : EFatura
        {
        }
        public sealed class QEFEFatura : EFatura
        {
        }

        ///-----------------------------------------------------------------------------------------------------------------------
        ///-GonderilenGuncelleByDate-----------------------------------------------------------------------------------------------
        public override void GonderilenGuncelleByDate(DateTime day1, DateTime day2)
        {

        }
        public sealed class FIT_ING_INGBANKEFatura : EFatura
        {

        }
        public sealed class DPlanetEFatura : EFatura
        {
        }
        public sealed class EDMEFatura : EFatura
        {
        }
        public sealed class QEFEFatura : EFatura
        {
            }
            ///-----------------------------------------------------------------------------------------------------------------------
            ///-GonderilenGuncelleByList-----------------------------------------------------------------------------------------------
            public override void GonderilenGuncelleByList(List<string> UUIDs)
            {

            }
public sealed class FIT_ING_INGBANKEFatura : EFatura
{
}
public sealed class DPlanetEFatura : EFatura
{
}
public sealed class EDMEFatura : EFatura
{
}
public sealed class QEFEFatura : EFatura

{
}

///-----------------------------------------------------------------------------------------------------------------------
///-GelenEsle-----------------------------------------------------------------------------------------------
public static void GelenEsle(List<string> uuids)
{

}

public sealed class FIT_ING_INGBANKEFatura : EFatura
{
}
public sealed class DPlanetEFatura : EFatura
{
}
public sealed class EDMEFatura : EFatura
{
}
public sealed class QEFEFatura : EFatura

{
}
            ///-----------------------------------------------------------------------------------------------------------------------


}
