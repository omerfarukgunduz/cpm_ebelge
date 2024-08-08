using System.Globalization;
using System;

namespace cpm_ebelge.Models.QEF
{
    public class eIrsaliye
    {

        public override string TopluGonder()
        {
            base.TopluGonder();
            var qefIrsaliye = new QEF.DespatchAdviceService();

            foreach (Irsaliye in Irsaliyeler)
            {
                int EVRAKSN = Convert.ToInt32(Irsaliye.AdditionalDocumentReference.Where(element => element.DocumentTypeCode.Value == "CUST_DES_ID").First().ID.Value);
                DespatchAdviceSerializer qefSerializer = new DespatchAdviceSerializer();
                eIrsaliyeProtectedValues.strFatura = qefSerializer.GetXmlAsString(Irsaliye);

                var qefResult = qefIrsaliye.IrsaliyeGonder(eIrsaliyeProtectedValues.strFatura, EVRAKSN, Irsaliye.IssueDate.Value.Date); ;

                var qefFaturaUBL = qefIrsaliye.IrsaliyeUBLIndir(new[] { Irsaliye.UUID.Value });

                Result.DurumAciklama = qefResult.aciklama ?? "";
                Result.DurumKod = qefResult.gonderimDurumu.ToString();
                Result.DurumZaman = DateTime.TryParse(qefResult.gonderimTarihi, out DateTime dz) ? dz : new DateTime(1900, 1, 1);
                Result.EvrakNo = qefResult.belgeNo;
                Result.UUID = qefResult.ettn;
                Result.ZarfUUID = "";

                Entegrasyon.UpdateEirsaliye(Result, EVRAKSN, qefFaturaUBL.First().Value);
                sb.AppendLine("e-İrsaliye başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo);
            }
            return sb.ToString();

        }

        public override string TopluGonder()
        {
            base.TopluGonder();
            var qefIrsaliye = new QEF.DespatchAdviceService();

            var qefResult = qefIrsaliye.IrsaliyeGonder(eIrsaliyeProtectedValues.strFatura, EVRAKSN, eIrsaliyeProtectedValues.doc.IssueDate.Value.Date);

            if (!string.IsNullOrEmpty(qefResult.aciklama))
                throw new Exception(qefResult.aciklama);

            var qefFaturaUBL = qefIrsaliye.IrsaliyeUBLIndir(new[] { eIrsaliyeProtectedValues.eIrsaliyeProtectedValues.doc.UUID.Value });

            Result.DurumAciklama = qefResult.aciklama ?? "";
            Result.DurumKod = qefResult.gonderimDurumu.ToString();
            Result.DurumZaman = DateTime.TryParse(qefResult.gonderimTarihi, out DateTime dz) ? dz : new DateTime(1900, 1, 1);
            Result.EvrakNo = qefResult.belgeNo;
            Result.UUID = qefResult.ettn;
            Result.ZarfUUID = "";

            Entegrasyon.UpdateEirsaliye(Result, EVRAKSN, qefFaturaUBL.First().Value);
            return "e-İrsaliye başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo;

        }

        public override string Esle()
        {
            base.Esle();
            var Result = new Results.EFAGDN();
            var qefIrsaliye = new QEF.DespatchAdviceService();

            var qefGonderilenler = qefIrsaliye.GonderilenEIrsaliyeIndir(new[] { GUID });

            foreach (var doc in qefGonderilenler)
            {

                Result.DurumAciklama = doc.Key.gonderimCevabiDetayi ?? "";
                Result.DurumKod = doc.Key.gonderimCevabiKodu + "";
                Result.DurumZaman = DateTime.TryParseExact(doc.Key.alimTarihi, "", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : new DateTime(1900, 1, 1);
                Result.EvrakNo = doc.Key.belgeNo;
                Result.UUID = doc.Key.ettn;
                Result.ZarfUUID = "";

                Entegrasyon.UpdateEfagdnStatus(Result);
            }
            break;

        }

        public override string Esle()
        {
            base.Esle();
            var Result = new Results.EFAGDN();
            var start = new DateTime(GonderimTarih.Year, GonderimTarih.Month, GonderimTarih.Day, 0, 0, 0);
            var end = new DateTime(GonderimTarih.Year, GonderimTarih.Month, GonderimTarih.Day, 23, 59, 59);
            var qefIrsaliye = new QEF.DespatchAdviceService();

            var qefGonderilenler = qefIrsaliye.GonderilenIrsaliyeler(start, end);

            foreach (var doc in qefGonderilenler)
            {
                if (doc.yerelBelgeNo == EVRAKSN.ToString())
                {
                    Result.DurumAciklama = doc.hataMesaji ?? "";
                    Result.DurumKod = "";
                    Result.DurumZaman = DateTime.TryParseExact(doc.alimZamani, "yyyyMMdd", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : new DateTime(1900, 1, 1);
                    Result.UUID = doc.ettn;

                    Entegrasyon.UpdateEIrsaliye(Result);
                    break;
                }
            }
            break;
        }

        public override string AlinanFaturalarListesi()
        {
            base.AlinanFaturalarListesi();
            var data = new List<AlinanBelge>();

            var qefIrsaliye = new QEF.GetDespatchAdviceService();

            foreach (var fatura in qefIrsaliye.GelenEIrsaliyeler(StartDate, EndDate))
            {
                data.Add(new AlinanBelge
                {
                    EVRAKGUID = Guid.Parse(fatura.Value.ettn),
                    EVRAKNO = fatura.Value.ettn,
                    YUKLEMEZAMAN = DateTime.ParseExact(fatura.Value.alimTarihi, "yyyyMMdd", CultureInfo.CurrentCulture),
                    GBETIKET = "",
                    GBUNVAN = ""
                });
            }
            break;
            return data;
        }

        public override string Indir()
        {
            base.Indir();
            var Result = new Results.EFAGLN();

            var qefFatura = new QEF.GetDespatchAdviceService();
            foreach (var fatura in qefFatura.GelenEIrsaliyeler(day1, day2))
            {
                Result.DurumAciklama = fatura.Value.yanitGonderimCevabiDetayi ?? "";
                Result.DurumKod = fatura.Value.yanitGonderimCevabiKodu + "";
                Result.DurumZaman = DateTime.ParseExact(fatura.Key.belgeTarihi, "yyyyMMdd", CultureInfo.CurrentCulture);
                Result.Etiket = fatura.Key.gonderenEtiket;
                Result.EvrakNo = fatura.Value.belgeNo;
                Result.UUID = fatura.Value.ettn;
                Result.VergiHesapNo = fatura.Key.gonderenVknTckn;
                Result.ZarfUUID = "";

                var ubl = ZipUtility.UncompressFile(qefFatura.GelenUBLIndir(fatura.Value.ettn));
                Entegrasyon.InsertIrsaliye(Result, ubl);
            }
            break;
        }

        public override string Kabul()
        {
            base.Kabul();
            throw new Exception("Entegratör Bu Eylemi Desteklememektedir!");
        }

        public override string Red()
        {
            base.Red();
            throw new Exception("Entegratör Bu Eylemi Desteklememektedir!");
        }

        public override string GonderilenGuncelleByDate()
        {
            base.GonderilenGuncelleByDate();
            var Result = new Results.EFAGDN();

            var qefIrsaliye = new QEF.DespatchAdviceService();
            var qefGelen = qefIrsaliye.GonderilenIrsaliyeler(start, end);

            //var edmYanitlar = edmFatura.GelenUygulamaYanitlari(day1, day2);

            foreach (var fatura in qefGelen)
            {
                if (fatura.alimDurumu == "İşlendi" && fatura.ettn != null)
                    Entegrasyon.UpdateEfagdnGonderimDurum(fatura.ettn, 3);
                else if (fatura.alimDurumu == "İşleme Hatası" && fatura.ettn != null)
                    Entegrasyon.UpdateEfagdnGonderimDurum(fatura.ettn, 0);

                if (fatura.ettn != null)
                {
                    Result.DurumAciklama = fatura.hataMesaji ?? "";
                    Result.DurumKod = "";
                    Result.DurumZaman = DateTime.TryParseExact(fatura.alimZamani, "yyyyMMdd", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : new DateTime(1900, 1, 1);
                    Result.UUID = fatura.ettn;
                    Result.ZarfUUID = "";

                    Entegrasyon.UpdateEfagdnStatus(Result);
                }
            }
            break;
        }

        public override string GonderilenGuncelle()
        {
            base.GonderilenGuncelle();
            var Result = new Results.EFAGDN();

            var qefIrsaliye = new QEF.DespatchAdviceService();

            var qefDosyalar = qefIrsaliye.IrsaliyeUBLIndir(UUIDs.ToArray());

            foreach (var dosya in qefDosyalar)
                Entegrasyon.UpdateEfadosIrsaliye(dosya.Value);
            break;
        }


        public override string YanitGonder()
        {

            base.YanitGonder();

        }
        public override string YanitGuncelle()
        {
            base.YanitGuncelle();
        }

        public override string GonderilenYanitlar()
        {
            base.GonderilenYanitlar();

        }

    }
}
