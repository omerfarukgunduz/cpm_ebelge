using cpm_ebelge.Models.BaseModels;
using System;

namespace cpm_ebelge.Models
{

    public class eIrsaliyeProtectedValues
    {
        public dynamic strFatura { get; set; }
        public dynamic doc { get; set; }

    }
    public abstract class eIrsaliye : BaseDocument
    {
        protected eIrsaliyeProtectedValues eIrsaliyeProtectedValues { get; set; } = new eIrsaliyeProtectedValues();

        public static string TopluGonder(string PK, List<DespatchAdviceType> Irsaliyeler, string ENTSABLON)
        {
            {
                StringBuilder sb = new StringBuilder();
                var Result = new Results.EFAGDN();

                if (Connector.m.SchematronKontrol)
                {
                    DespatchAdviceSerializer ser = new DespatchAdviceSerializer();
                    foreach (var Irsaliye in Irsaliyeler)
                    {
                        var schematronResult = SchematronChecker.Check(Irsaliye, SchematronDocType.eIrsaliye);
                        if (schematronResult.SchemaResult != "Başarılı" || schematronResult.SchematronResult != "Başarılı")
                            throw new Exception(schematronResult.Detail);
                    }
                }

            }


        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }

        public static string Gonder(int EVRAKSN)
        {
            DespatchAdvice despatchAdvice = new DespatchAdvice();
            eIrsaliyeProtectedValues.doc = despatchAdvice.CreateDespactAdvice(EVRAKSN);

            if (UrlModel.SelectedItem == "QEF" && string.IsNullOrEmpty(eIrsaliyeProtectedValues.doc.ID.Value) && Connector.m.SablonTip)
            {
                var sablon = string.IsNullOrEmpty(despatchAdvice.ENTSABLON) ? Connector.m.Sablon : despatchAdvice.ENTSABLON;
                var notes = eIrsaliyeProtectedValues.doc.Note.ToList();
                notes.Add(new UblDespatchAdvice.NoteType { Value = $"#EFN_SERINO_TERCIHI#{sablon}#" });

                eIrsaliyeProtectedValues.doc.Note = notes.ToArray();
            }

            if (Connector.m.SchematronKontrol)
            {
                schematronResult = SchematronChecker.Check(eIrsaliyeProtectedValues.doc, SchematronDocType.eIrsaliye);
                if (schematronResult.SchemaResult != "Başarılı" || schematronResult.SchematronResult != "Başarılı")
                    throw new Exception(schematronResult.Detail);
            }

            UBLBaseSerializer serializer = new DespatchAdviceSerializer();
            eIrsaliyeProtectedValues.strFatura = serializer.GetXmlAsString(eIrsaliyeProtectedValues.doc);

            Connector.m.PkEtiketi = despatchAdvice.PK;
            Entegrasyon.EfagdnUuid(EVRAKSN, eIrsaliyeProtectedValues.doc.UUID.Value);


            var Result = new Results.EFAGDN();

        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }
        public static void Esle(string GUID)
        {

        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }
        public static void Esle(int EVRAKSN, DateTime GonderimTarih, string EVRAKGUID)

        {

        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }
        public static List<AlinanBelge> AlinanFaturalarListesi(DateTime StartDate, DateTime EndDate)
        {

        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
           
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }
        public static void Indir(DateTime day1, DateTime day2)
        {
        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }

        public static void Kabul(string GUID, string Aciklama)
        {

        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
           
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }
        public static void Red(string GUID, string Aciklama)
        {
        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
           
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }
        public static void GonderilenGuncelleByDate(DateTime start, DateTime end)
        {
        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }

        public static void GonderilenGuncelle(List<string> UUIDs)
        {
        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
           
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }
        /// SADECE EIRSALIYEDE OLANLAR 
        public static void YanitGonder(ReceiptAdviceType Yanit)
        {
            ReceiptAdviceSerializer serializer = new ReceiptAdviceSerializer();
            var docStr = serializer.GetXmlAsString(Yanit);
            eIrsaliyeProtectedValues.doc = Encoding.UTF8.GetBytes(docStr);

        }

      public static void YanitGuncelle(string UUID, string ZarfGuid)
      {

       }
  
        public static void GonderilenYanitlar(string UUID)
        {
            
         }
        
   

