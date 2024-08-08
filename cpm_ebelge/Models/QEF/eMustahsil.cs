using System;

namespace cpm_ebelge.Models.QEF
{
    public class eMustahsil
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

        public override string Iptal()
        {
            base.Iptal();
            var qefEMustahsil = new QEF.MustahsilService();
            var qefpResult = qefEMustahsil.IptalFatura(UUID);

            Entegrasyon.SilEarsiv(EVRAKSN, TOTAL, qefpResult.resultText, DateTime.Now);

            return "e-Müstahsil Makbuzu başarıyla iptal edildi.";
        }


    }
}
