namespace cpm_ebelge.Models.FIT
{
    public class eArsiv
    {
        public override string Gonder()
        {
            //
            base.Gonder();
            var fitEArsiv = new FIT.ArchiveWebService();
            var fitResult = fitEArsiv.EArsivGonder(eArsivProtectedValues.strFatura, eArsivProtectedValues.createdUBL.UUID.Value, doc.SUBE);
            byte[] ByteData = null;
            if (fitResult.Result.Result1 == ResultType.SUCCESS)
            {
                Connector.m.FaturaUUID = fitResult.preCheckSuccessResults[0].UUID;
                Connector.m.FaturaID = fitResult.preCheckSuccessResults[0].InvoiceNumber;
                //var fitFaturaUBL = fitEArsiv.FaturaUBLIndir();
                //ByteData = fitFaturaUBL.binaryData;

                Result.DurumAciklama = fitResult.preCheckSuccessResults[0].SuccessDesc;
                Result.DurumKod = fitResult.preCheckSuccessResults[0].SuccessCode + "";
                Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                Result.EvrakNo = fitResult.preCheckSuccessResults[0].InvoiceNumber;
                Result.UUID = fitResult.preCheckSuccessResults[0].UUID;
                Result.ZarfUUID = "";
                Result.YanitDurum = 0;

                Entegrasyon.UpdateEfagdn(Result, EVRAKSN, null);
                if (Connector.m.DokumanIndir)
                {
                    var Gonderilen = fitEArsiv.ImzaliIndir(Result.UUID, "", 0);
                    Entegrasyon.UpdateEfados(Gonderilen.binaryData);
                }
            }
            if (fitResult.Result.Result1 == ResultType.SUCCESS)
            {
                if (eArsivProtectedValues.doc.PRINT)
                {
                    response.KagitNusha = true;
                    response.Mesaj = "e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + fitResult.preCheckSuccessResults[0].InvoiceNumber + "\nYazdırmak İster Misiniz?";
                    response.Dosya = ByteData;
                }
                else
                {
                    response.KagitNusha = false;
                    response.Mesaj = "e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + fitResult.preCheckSuccessResults[0].InvoiceNumber;
                    response.Dosya = null;
                }
            }
            else
            {
                throw new Exception(fitResult.preCheckErrorResults[0].ErrorDesc);
            }
            return response;

        }
    }
}
