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
            public override string Gonder()
            {
                base.Gonder();
                var ingEMustahsil = new FIT.MmWebService();
                var fitResult = ingEMustahsil.EMmGonder(docs, eMustahsilProtectedValues.createdUBL.UUID.Value);
                var fitDosya = ingEMustahsil.MmUBLIndir(fitResult[0].ID, fitResult[0].UUID);

                Result.DurumAciklama = fitDosya[0].ResultDescription;
                Result.DurumKod = fitDosya[0].Result + "";
                Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                Result.EvrakNo = fitDosya[0].ID;
                Result.UUID = fitDosya[0].UUID;
                Result.ZarfUUID = "";

                Entegrasyon.UpdateMustahsil(Result, EVRAKSN, ZipUtility.UncompressFile(fitDosya[0].DocData));
                return "e-Müstahsil Makbuzu başarıyla gönderildi. Makbuz ID:" + fitResult[0].ID;
            }
        }
        public sealed class DPlanetEMustahsil : EMustahsil
        {
            public override string Gonder()
            {
                base.Gonder();
                eMustahsilProtectedValues.createdUBL.CreditNoteTypeCode = null;
                eMustahsilProtectedValues.createdUBL.DocumentCurrencyCode = null;
                eMustahsilProtectedValues.createdUBL.TaxCurrencyCode = null;
                strFatura = serializer.GetXmlAsString(eMustahsilProtectedValues.createdUBL); // XML byte tipinden string tipine dönüştürülür.
                var dpEMustahsil = new DigitalPlanet.MustahsilWebService();
                var dpResult = dpEMustahsil.MustahsilGonder(strFatura);

                if (dpResult.ServiceResult == COMMON.dpMustahsil.Result.Error)
                    throw new Exception(dpResult.ServiceResultDescription);

                Result.DurumAciklama = dpResult.Receipts[0].StatusDescription;
                Result.DurumKod = dpResult.Receipts[0].StatusCode + "";
                Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                Result.EvrakNo = dpResult.Receipts[0].ReceiptId;
                Result.UUID = dpResult.Receipts[0].UUID;
                Result.ZarfUUID = "";

                Entegrasyon.UpdateMustahsil(Result, EVRAKSN, dpResult.Receipts[0].ReturnValue);
                return "e-Müstahsil Makbuzu başarıyla gönderildi. Makbuz ID:" + dpResult.Receipts[0].ReceiptId;

            }
        }

        public sealed class EDMEMustahsil : EMustahsil
        {
            public override string Gonder()
            {
                base.Gonder();
                var edmEMustahsil = new EDM.MustahsilWebService();
                var edmResult = edmEMustahsil.MustahsilGonder(strFatura);

                var edmMmUbl = edmEMustahsil.MustahsilIndir(edmResult.MM[0].UUID);

                Result.DurumAciklama = edmMmUbl[0].HEADER.STATUS_DESCRIPTION;
                Result.DurumKod = "";
                Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                Result.EvrakNo = edmMmUbl[0].ID;
                Result.UUID = edmMmUbl[0].UUID;
                Result.ZarfUUID = "";

                Entegrasyon.UpdateMustahsil(Result, EVRAKSN, edmMmUbl[0].CONTENT.Value);
                return "e-Müstahsil Makbuzu başarıyla gönderildi. Makbuz ID:" + Result.EvrakNo;

            }
        }

        public sealed class QEFEMustahsil : EMustahsil
        {
            public override string Gonder()
            {
                base.Gonder();
                var sube = mm.SUBE;
                sube = sube == "default" ? "DFLT" : sube;

                var qefEMustahsil = new QEF.MustahsilService();
                var qefResult = qefEMustahsil.MustahsilGonder(strFatura, Connector.m.VknTckn, EVRAKSN, sube);

                if (appConfig.Debugging)
                {
                    if (!Directory.Exists("C:\\ReqResp\\QEF"))
                        Directory.CreateDirectory("C:\\ReqResp\\QEF");

                    File.WriteAllText("C:\\ReqResp\\QEF\\Resp_" + Guid.NewGuid() + ".json", JsonConvert.SerializeObject(qefResult));
                }

                var ByteData = qefResult.Belge.belgeIcerigi;

                Result.DurumAciklama = qefResult.Result.resultText;
                Result.DurumKod = qefResult.Result.resultCode;
                Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                Result.EvrakNo = qefResult.Result.resultExtra.First(elm => elm.key.ToString() == "belgeNo").value.ToString();
                Result.UUID = qefResult.Result.resultExtra.First(elm => elm.key.ToString() == "uuid").value.ToString();
                Result.ZarfUUID = "";
                Result.YanitDurum = 0;

                Entegrasyon.UpdateEfagdn(Result, EVRAKSN, ByteData, t: typeof(UBL.UBLObject.MmObject.CreditNoteType));

                return "e-Müstahsil Makbuzu başarıyla gönderildi. Makbuz ID:" + Result.EvrakNo;

            }
        }



        public static string Iptal(int EVRAKSN, string UUID, string EVRAKNO, decimal TOTAL)
        {
        }
        public sealed class FIT_ING_INGBANKEMustahsil : EMustahsil
        {
            public override string Iptal()
            {

                base.Iptal();
                var ingEMustahsil = new FIT.MmWebService();
                var fitResult = ingEMustahsil.EMmIptal(EVRAKNO, TOTAL);
                Entegrasyon.SilEarsiv(EVRAKSN, TOTAL, fitResult[0].ResultDescription, DateTime.Now);
                return fitResult[0].ResultDescription;
            }
        }
        public sealed class DPlanetEMustahsil : EMustahsil
        {
            public override string Iptal()
            {
                base.Iptal();
                var dpEMustahsil = new DigitalPlanet.MustahsilWebService();
                var dpResult = dpEMustahsil.MustahsilIptal(UUID, TOTAL);
                if (dpResult.ServiceResult == COMMON.dpMustahsil.Result.Error)
                    throw new Exception(dpResult.ServiceResultDescription);
                Entegrasyon.SilEarsiv(EVRAKSN, TOTAL, dpResult.StatusDescription, DateTime.Now);
                return "e-Müstahsil Makbuzu başarıyla iptal edildi. Makbuz ID:" + dpResult.ReceiptId;

            }
        }
        public sealed class EDMEMustahsil : EMustahsil
        {
            public override string Iptal()
            {
                base.Iptal();
                var edmEMustahsil = new EDM.MustahsilWebService();
                var edmResult = edmEMustahsil.MustahsilIptal(UUID, TOTAL);

                Entegrasyon.SilEarsiv(EVRAKSN, TOTAL, "", DateTime.Now);
                return "e-Müstahsil Makbuzu başarıyla iptal edildi.";
            }
        }
        public sealed class QEFEMustahsil : EMustahsil
        {
            public override string Iptal()
            {
                base.Iptal();
                var qefEMustahsil = new QEF.MustahsilService();
                var qefpResult = qefEMustahsil.IptalFatura(UUID);

                Entegrasyon.SilEarsiv(EVRAKSN, TOTAL, qefpResult.resultText, DateTime.Now);

                return "e-Müstahsil Makbuzu başarıyla iptal edildi.";
            }
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
