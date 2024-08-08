using System;

namespace cpm_ebelge.Models.QEF
{
    public class eArsiv
    {
        public override string Gonder()
        {
            base.Gonder();
            var qefEArsiv = new QEF.ArchiveService();
            eArsivProtectedValues.doc.SUBE = eArsivProtectedValues.doc.SUBE == "default" ? "DFLT" : eArsivProtectedValues.doc.SUBE;
            var qefResult = qefEArsiv.EArsivGonder(eArsivProtectedValues.strFatura, Connector.m.VknTckn, EVRAKSN, eArsivProtectedValues.doc.SUBE, eArsivProtectedValues.doc.BaseUBL.IssueDate.Value.Date);

            if (qefResult.Result.resultText != "İşlem başarılı.")
                throw new Exception(qefResult.Result.resultText);

            ByteData = qefResult.Belge.belgeIcerigi;

            Result.DurumAciklama = qefResult.Result.resultText;
            Result.DurumKod = qefResult.Result.resultCode;
            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
            Result.EvrakNo = qefResult.Result.resultExtra.First(elm => elm.key.ToString() == "faturaNo").value.ToString();
            Result.UUID = qefResult.Result.resultExtra.First(elm => elm.key.ToString() == "uuid").value.ToString();
            Result.ZarfUUID = "";
            Result.YanitDurum = 0;

            Entegrasyon.UpdateEfagdn(Result, EVRAKSN, ByteData);

            if (eArsivProtectedValues.doc.PRINT)
            {
                response.KagitNusha = true;
                response.Mesaj = "e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo + "\nYazdırmak İster Misiniz?";
                response.Dosya = ByteData;
            }
            else
            {
                response.KagitNusha = false;
                response.Mesaj = "e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo;
                response.Dosya = null;
            }

            return response;
        }



        public override string TopluGonder()
        {
            base.TopluGonder();
            var qefEArsiv = new QEF.ArchiveService();

            foreach (var fatura in Faturalar)
            {
                if (fatura.Note == null)
                    fatura.Note = new List<UblInvoiceObject.NoteType>().ToArray();

                var noteList = fatura.Note.ToList();
                noteList.Add(new UblInvoiceObject.NoteType { Value = "Gönderim Şekli: ELEKTRONIK" });

                fatura.Note = noteList.ToArray();

                UBLBaseSerializer serializer = new InvoiceSerializer();  // UBL  XML e dönüştürülür
                eArsivProtectedValues.strFatura = serializer.GetXmlAsString(fatura); // XML byte tipinden string tipine dönüştürülür

                var sube = "DFLT";

                if (Subeler.Count > 0)
                    sube = Subeler[0];

                sube = sube == "default" ? "DFLT" : sube;
                var EVRAKSN = Convert.ToInt32(fatura.AdditionalDocumentReference.Where(element => element.DocumentTypeCode.Value == "CUST_INV_ID").First().ID.Value);
                var qefResult = qefEArsiv.EArsivGonder(eArsivProtectedValues.strFatura, Connector.m.VknTckn, EVRAKSN, sube, fatura.IssueDate.Value.Date);

                if (qefResult.Result.resultText != "İşlem başarılı.")
                    throw new Exception(qefResult.Result.resultText);

                var ByteData = qefResult.Belge.belgeIcerigi;

                Result.DurumAciklama = qefResult.Result.resultText;
                Result.DurumKod = qefResult.Result.resultCode;
                Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                Result.EvrakNo = qefResult.Result.resultExtra.First(elm => elm.key.ToString() == "faturaNo").value.ToString();
                Result.UUID = qefResult.Result.resultExtra.First(elm => elm.key.ToString() == "uuid").value.ToString();
                Result.ZarfUUID = "";
                Result.YanitDurum = 0;

                Entegrasyon.UpdateEfagdn(Result, EVRAKSN, ByteData);

                sb.AppendLine("e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo);
            }
            return sb.ToString();
        }

        public override string Iptal()
        {
            base.Iptal();
            var qefEArsiv = new QEF.ArchiveService();
            var qefpResult = qefEArsiv.IptalFatura(GUID);

            if (qefpResult.resultText != "İşlem başarılı.")
                throw new Exception(qefpResult.resultText);

            Entegrasyon.SilEarsiv(EVRAKSN, TUTAR, qefpResult.resultText, DateTime.Now);

            return "eArşiv Fatura Başarıyla İptal Edilmiştir ve İlgili eArşiv Fatura Silinmiştir!";
        }

        public override string Itiraz()
        {
            base.Itiraz();
            //var edmArsiv = new EDM.ArchiveWebService();
            //edmArsiv.WebServisAdresDegistir();
            //edmArsiv.EArsivItiraz();
            return "Test!";
        }


    }
}
