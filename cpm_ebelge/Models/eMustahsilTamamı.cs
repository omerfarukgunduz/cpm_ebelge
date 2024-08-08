using cpm_ebelge.Models.BaseModels;
using System.Text;
using System.Xml.Serialization;
using System;

namespace cpm_ebelge.Models
{
    public class eMustahsilProtectedValues
    {
        public dynamic createdUBL { get; set; }
        public string schematronResult { get; set; }
        public string strFatura { get; set; }
        //public string Result { get; set; }

    }
    public abstract class EMustahsil : EArsiv_EMustahsil
    {
        protected eMustahsilProtectedValues eMustahsilProtectedValues { get; set; } = new eMustahsilProtectedValues();

        public static string Gonder(int EVRAKSN)
        {
            MmUBL mm = new MmUBL();
            eMustahsilProtectedValues.createdUBL = mm.CreateCreditNote(EVRAKSN);  // e-Mm  UBL i oluşturulur
            Connector.m.PkEtiketi = mm.PK;
            if (Connector.m.SchematronKontrol)
            {
                eMustahsilProtectedValues.schematronResult = SchematronChecker.Check(eMustahsilProtectedValues.createdUBL, SchematronDocType.eArsiv);
                if (schematronResult.SchemaResult != "Başarılı" || schematronResult.SchematronResult != "Başarılı")
                    throw new Exception(schematronResult.Detail);
            }
            UBLBaseSerializer serializer = new MmSerializer();  // UBL  XML e dönüştürülür
            eMustahsilProtectedValues.strFatura = serializer.GetXmlAsString(eMustahsilProtectedValues.createdUBL); // XML byte tipinden string tipine dönüştürülür.


            var docs = new Dictionary<object, byte[]>();
            docs.Add(eMustahsilProtectedValues.createdUBL.UUID.Value + ".xml", Encoding.UTF8.GetBytes(strFatura));

            eMustahsilProtectedValues Result = new Results.EFAGDN();

            ///



        }
        public sealed class FIT_ING_INGBANKEMustahsil : EMustahsil
        {
            
        }
        public sealed class DPlanetEMustahsil : EMustahsil
        {
            
        }

        public sealed class EDMEMustahsil : EMustahsil
        {
            
        }

        public sealed class QEFEMustahsil : EMustahsil
        {
            
        }



        public static string Iptal(int EVRAKSN, string UUID, string EVRAKNO, decimal TOTAL)
        {
        }
        public sealed class FIT_ING_INGBANKEMustahsil : EMustahsil
        {
           
        }
        public sealed class DPlanetEMustahsil : EMustahsil
        {
           
        }
        public sealed class EDMEMustahsil : EMustahsil
        {
            
        }
        public sealed class QEFEMustahsil : EMustahsil
        {
            
        }

        //Esle yok.
        public override void Esle()
        {
            throw new NotImplementedException();
        }
        //Itiraz yok.
        public override string Itiraz()
        {
            throw new NotImplementedException();
        }
        //TopluGonder yok.
        public override string TopluGonder()
        {
            throw new NotImplementedException();
        }
    }

}
}
