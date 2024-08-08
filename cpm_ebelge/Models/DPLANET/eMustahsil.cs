using System;

namespace cpm_ebelge.Models.DPLANET
{
    public class eMustahsil
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

        public override string Esle()
        {
        }
        public override string Itiraz()
        {
        }
        public override string TopluGonder()
        {
        }
    }
}
