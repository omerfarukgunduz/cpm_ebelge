namespace cpm_ebelge.Models.FIT
{
    public class eMustahsil
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

        public override string Iptal()
        {

            base.Iptal();
            var ingEMustahsil = new FIT.MmWebService();
            var fitResult = ingEMustahsil.EMmIptal(EVRAKNO, TOTAL);
            Entegrasyon.SilEarsiv(EVRAKSN, TOTAL, fitResult[0].ResultDescription, DateTime.Now);
            return fitResult[0].ResultDescription;
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
