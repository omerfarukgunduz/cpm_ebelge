using System.Runtime.ConstrainedExecution;

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



        public override string TopluGonder()
        {

            base.TopluGonder();
            var fitEArsiv = new FIT.ArchiveWebService();
            fitEArsiv.WebServisAdresDegistir();
            //Connector.m.PkEtiketi = PKETIKET;
            if (!FarkliSube)
            {
                var fitResult = fitEArsiv.TopluEArsivGonder(Faturalar, Subeler[0]);

                foreach (var a in fitResult.preCheckSuccessResults)
                {
                    foreach (var eArsivProtectedValues.doc in Faturalar)
                            {
                        if (eArsivProtectedValues.doc.UUID.Value == a.UUID)
                        {
                            eArsivProtectedValues.doc.ID.Value = a.InvoiceNumber;
                            sb.AppendLine("e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + a.InvoiceNumber);
                            eArsivProtectedValues.doc.ID.Value = a.InvoiceNumber;

                            Result.DurumAciklama = a.SuccessDesc;
                            Result.DurumKod = a.SuccessCode + "";
                            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                            Result.EvrakNo = a.InvoiceNumber;
                            Result.UUID = a.UUID;
                            Result.ZarfUUID = "";
                            Result.YanitDurum = 0;

                            Entegrasyon.UpdateEfagdn(Result, Convert.ToInt32(eArsivProtectedValues.doc.AdditionalDocumentReference.Where(element => element.DocumentTypeCode?.Value == "CUST_INV_ID").First().ID.Value), ser.GetXmlAsByteArray(eArsivProtectedValues.doc));
                            break;
                        }
                    }
                }
                foreach (var b in fitResult.preCheckErrorResults)
                {
                    sb.AppendLine(String.Format("Hatalı e-Arşiv Fatura:{0} - Hata:{1}({2})", b.InvoiceNumber, b.ErrorDesc, b.ErrorCode));
                }
                if (fitResult.Result.Result1 == ResultType.FAIL)
                    sb.AppendLine(fitResult.Detail);
            }
            else
            {
                int i = 0;
                foreach (var fatura in Faturalar)
                {
                    UBLBaseSerializer serializer = new InvoiceSerializer();  // UBL  XML e dönüştürülür
                    eArsivProtectedValues.strFatura = serializer.GetXmlAsString(fatura); // XML byte tipinden string tipine dönüştürülür

                    var fitResult = fitEArsiv.EArsivGonder(eArsivProtectedValues.strFatura, fatura.UUID.Value, Subeler[i]);
                    i++;

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
                    sb.AppendLine("e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo);

                    Entegrasyon.UpdateEfagdn(Result, Convert.ToInt32(fatura.AdditionalDocumentReference.Where(element => element.DocumentTypeCode.Value == "CUST_INV_ID").First().ID.Value), null);
                    if (Connector.m.DokumanIndir)
                    {
                        var Gonderilen = fitEArsiv.ImzaliIndir(Result.UUID, "", 0);
                        Entegrasyon.UpdateEfados(Gonderilen.binaryData);
                    }
                }
            }
            return sb.ToString();
        }



        public override string Iptal()
        {

            base.Iptal();
            var fitEArsiv = new FIT.ArchiveWebService();
            var fitResult = fitEArsiv.EArsivIptal(EVRAKNO, TUTAR);
            if (fitResult.Result.Result1 == ResultType.SUCCESS)
            {
                Entegrasyon.SilEarsiv(EVRAKSN, TUTAR, fitResult.invoiceCancellation.message, DateTime.Now);
            }
            else
            {
                throw new Exception(fitResult.invoiceCancellation.message);
            }
            return fitResult.invoiceCancellation.message + " ve İlgili eArşiv Fatura Silinmiştir!";
        }

        public override string Itiraz()
        {

            base.Itiraz();
            var fitEArsiv = new FIT.ArchiveWebService();
            var fitResult = fitEArsiv.EArsivItiraz(EVRAKNO, Objection);
            if (fitResult.Result == ResultType.FAIL)
            {
                throw new Exception(fitResult.Detail);
            }
            return fitResult.Detail;
        }


    }
}
