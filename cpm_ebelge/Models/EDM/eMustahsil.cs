using System;

namespace cpm_ebelge.Models.EDM
{
    public class eMustahsil
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

        public override string Iptal()
        {
            base.Iptal();
            var edmEMustahsil = new EDM.MustahsilWebService();
            var edmResult = edmEMustahsil.MustahsilIptal(UUID, TOTAL);

            Entegrasyon.SilEarsiv(EVRAKSN, TOTAL, "", DateTime.Now);
            return "e-Müstahsil Makbuzu başarıyla iptal edildi.";
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
