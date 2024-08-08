using System.Data;
using System;
using System.Globalization;
using System.Xml.Serialization;

namespace cpm_ebelge.Models.QEF
{
    public class eFatura
    {
        public override string Gonder()
        {
            base.Gonder();
            var qefEFatura = new QEF.InvoiceService();
            strFatura = serializer.GetXmlAsString(createdUBL);
            var qefResult = qefEFatura.FaturaGonder(strFatura, EVRAKSN, createdUBL.IssueDate.Value);

            if (qefResult.durum == 2)
                throw new Exception("Bir Hata Oluştu!\n" + qefResult.aciklama);

            Result.DurumAciklama = qefResult.aciklama ?? "";
            Result.DurumKod = qefResult.gonderimDurumu.ToString();
            Result.DurumZaman = DateTime.TryParse(qefResult.gonderimTarihi, out DateTime dz) ? dz : new DateTime(1900, 1, 1);
            Result.EvrakNo = qefResult.belgeNo;
            Result.UUID = qefResult.ettn;
            Result.ZarfUUID = "";
            Result.YanitDurum = doc.METHOD == "TEMELFATURA" ? 1 : 0;

            Entegrasyon.UpdateEfagdn(Result, EVRAKSN, null);
            if (Connector.m.DokumanIndir)
            {
                var Gonderilen = qefEFatura.FaturaUBLIndir(new[] { Result.UUID });
                Entegrasyon.UpdateEfados(Gonderilen.First().Value);
            }
            return "e-Fatura başarıyla gönderildi. \nEvrak No: " + qefResult.belgeNo;
        }


        public override string TopluGonder()
        {
            base.TopluGonder();
            var qefFatura = new QEF.InvoiceService();

            foreach (var Fatura in Faturalar)
            {
                if (Connector.m.SablonTip)
                {
                    var createdUBL = Fatura.BaseUBL;
                    var sablon = string.IsNullOrEmpty(Fatura.ENTSABLON) ? Connector.m.Sablon : Fatura.ENTSABLON;

                    if (createdUBL.Note == null)
                        createdUBL.Note = new UblInvoiceObject.NoteType[0];

                    var list = createdUBL.Note.ToList();
                    list.Add(new UblInvoiceObject.NoteType { Value = $"#EFN_SERINO_TERCIHI#{sablon}#" }); ;
                    createdUBL.Note = list.ToArray();
                }

                if (Fatura.BaseUBL.ProfileID.Value == "IHRACAT")
                    Connector.m.PkEtiketi = "urn:mail:ihracatpk@gtb.gov.tr";
                else
                    Connector.m.PkEtiketi = Fatura.PK;

                var strFatura = serializer.GetXmlAsString(Fatura.BaseUBL); // XML byte tipinden string tipine dönüştürülür

                var qefResult = qefFatura.FaturaGonder(strFatura, Convert.ToInt32(Fatura.BaseUBL.AdditionalDocumentReference.Where(element => element.DocumentTypeCode.Value == "CUST_INV_ID").First().ID.Value), Fatura.BaseUBL.IssueDate.Value);

                if (qefResult.durum == 2)
                    throw new Exception("Bir Hata Oluştu!\n" + qefResult.aciklama);

                Result.DurumAciklama = qefResult.aciklama ?? "";
                Result.DurumKod = qefResult.gonderimDurumu.ToString();
                Result.DurumZaman = DateTime.TryParse(qefResult.gonderimTarihi, out DateTime dz) ? dz : new DateTime(1900, 1, 1);
                Result.EvrakNo = qefResult.belgeNo;
                Result.UUID = qefResult.ettn;
                Result.ZarfUUID = "";
                Result.YanitDurum = Fatura.METHOD == "TEMELFATURA" ? 1 : 0;

                var qefFaturaResult = qefFatura.FaturaUBLIndir(new[] { Fatura.BaseUBL.UUID.Value });
                int EVRAKSN = Entegrasyon.GetEvraksnFromUUID(new List<string> { Result.UUID })[0];

                Entegrasyon.UpdateEfagdn(Result, EVRAKSN, qefFaturaResult.First().Value);
                sb.AppendLine("e-Fatura başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo);
            }
            return sb.ToString();
        }

        public override string Esle()
        {
            base.Esle();
            var qefFatura = new QEF.InvoiceService();

            foreach (var d in Value)
            {
                var qefGelen = qefFatura.GonderilenEFaturaIndir(Entegrasyon.GetUUIDFromEvraksn(Value), Value.Select(elm => elm + "").ToArray());

                foreach (var doc in qefGelen)
                {
                    Result.DurumAciklama = doc.Key.gonderimCevabiDetayi ?? "";
                    Result.DurumKod = doc.Key.gonderimCevabiKodu + "";
                    Result.DurumZaman = DateTime.TryParseExact(doc.Key.alimTarihi, "", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : new DateTime(1900, 1, 1);
                    Result.EvrakNo = doc.Key.belgeNo;
                    Result.UUID = doc.Key.ettn;
                    Result.ZarfUUID = "";
                    Result.YanitDurum = doc.Key.yanitDurumu == 1 ? 3 : (doc.Key.yanitDurumu == 2 ? 2 : 0);

                    Entegrasyon.UpdateEfagdn(Result, d, doc.Value, true);

                    Result.DurumAciklama = doc.Key.yanitDetayi ?? "";
                    Result.DurumKod = doc.Key.yanitDurumu == 1 ? "3" : (doc.Key.yanitDurumu == 2 ? "2" : "0");
                    Result.DurumZaman = DateTime.TryParseExact(doc.Key.yanitTarihi, "", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt2) ? dt2 : new DateTime(1900, 1, 1);
                    Result.UUID = doc.Key.ettn;

                    //File.WriteAllText($"C:\\QEF\\{Guid.NewGuid()}.json", JsonConvert.SerializeObject(doc.Key));
                    //File.WriteAllText($"C:\\QEF\\{Guid.NewGuid()}.json", JsonConvert.SerializeObject(Result));

                    Entegrasyon.UpdateEfagdnStatus(Result);
                    if (doc.Key.durum == 3 && doc.Key.gonderimDurumu == 4 && doc.Key.gonderimCevabiKodu == 1200)
                        Entegrasyon.UpdateEfagdnGonderimDurum(doc.Key.ettn, 3);
                    else if (doc.Key.gonderimCevabiKodu >= 1200 && doc.Key.gonderimCevabiKodu < 1300)
                        Entegrasyon.UpdateEfagdnGonderimDurum(doc.Key.ettn, 2);
                    else if (doc.Key.gonderimCevabiKodu == 1300)
                        Entegrasyon.UpdateEfagdnGonderimDurum(doc.Key.ettn, 3);
                    else if (doc.Key.gonderimDurumu > 2)
                        Entegrasyon.UpdateEfagdnGonderimDurum(doc.Key.ettn, 0);
                }
            }
            break;
        }

        public override string AlinanFaturalarListesi()
        {
            base.AlinanFaturalarListesi();
            var data = new List<AlinanBelge>();

            var qefFatura = new QEF.GetInvoiceService();
            qefFatura.GelenEfaturalar(StartDate, EndDate);

            foreach (var fatura in qefFatura.GelenEfaturalar(StartDate, EndDate))
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
            var qefGelenler = new List<Results.EFAGLN>();
            var qefGelenlerByte = new List<byte[]>();

            var qefFatura = new QEF.GetInvoiceService();
            foreach (var fatura in qefFatura.GelenEfaturalar(day1, day2))
            {
                qefGelenler.Add(new Results.EFAGLN
                {
                    DurumAciklama = fatura.Value.yanitGonderimCevabiDetayi ?? "",
                    DurumKod = fatura.Value.yanitGonderimCevabiKodu + "",
                    DurumZaman = DateTime.ParseExact(fatura.Key.belgeTarihi, "yyyyMMdd", CultureInfo.CurrentCulture),
                    Etiket = fatura.Key.gonderenEtiket,
                    EvrakNo = fatura.Value.belgeNo,
                    UUID = fatura.Value.ettn,
                    VergiHesapNo = fatura.Key.gonderenVknTckn,
                    ZarfUUID = ""
                });
                var ubl = ZipUtility.UncompressFile(qefFatura.GelenUBLIndir(fatura.Value.ettn));
                qefGelenlerByte.Add(ubl);
            }
            Entegrasyon.InsertEfagln(qefGelenlerByte.ToArray(), qefGelenler.ToArray());
            break;
        }

        public override string Kabul()
        {
            base.Kabul();
            var qefAdp = new RemoteSqlDataAdapter("SELECT DOSYA FROM EFAGLN WITH (NOLOCK) WHERE EVRAKGUID = @GUID", appConfig.GetConnectionStrings()[0]);
            qefAdp.SelectCommand.Parameters.AddWithValue("@GUID", GUID);

            DataTable qefDt = new DataTable();
            qefAdp.Fill(ref qefDt);

            XmlSerializer qefSer = new XmlSerializer(typeof(UblInvoiceObject.InvoiceType));
            var qefParty = ((UblInvoiceObject.InvoiceType)qefSer.Deserialize(new MemoryStream((byte[])qefDt.Rows[0][0]))).AccountingSupplierParty;

            var qefUygulamaYaniti = new QEF.GetInvoiceService();
            var qefResult = qefUygulamaYaniti.YanıtGonder(GUID, true, Aciklama, qefParty.Party);

            if (qefResult.durum == 2)
                throw new Exception(qefResult.aciklama);

            Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { ZarfUUID = qefResult.ettn }, GUID, true, Aciklama);
        }


        public override string Red()
        {
            base.Red();
            var qefAdp = new RemoteSqlDataAdapter("SELECT DOSYA FROM EFAGLN WITH (NOLOCK) WHERE EVRAKGUID = @GUID", appConfig.GetConnectionStrings()[0]);
            qefAdp.SelectCommand.Parameters.AddWithValue("@GUID", GUID);

            DataTable qefDt = new DataTable();
            qefAdp.Fill(ref qefDt);

            XmlSerializer qefSer = new XmlSerializer(typeof(UblInvoiceObject.InvoiceType));
            var qefParty = ((UblInvoiceObject.InvoiceType)qefSer.Deserialize(new MemoryStream((byte[])qefDt.Rows[0][0]))).AccountingSupplierParty;

            var qefUygulamaYaniti = new QEF.GetInvoiceService();
            var qefResult = qefUygulamaYaniti.YanıtGonder(GUID, false, Aciklama, qefParty.Party);

            if (qefResult.durum == 2)
                throw new Exception(qefResult.aciklama);

            Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { UUID = qefResult.ettn }, GUID, false, Aciklama);
        }

        public override string GonderilenGuncelleByDate()

        {
            base.GonderilenGuncelleByDate();
            var Response = new Results.EFAGDN();
            var qefFatura = new QEF.InvoiceService();
            var qefGelen = qefFatura.GonderilenFaturalar(day1, day2);

            //var edmYanitlar = edmFatura.GelenUygulamaYanitlari(day1, day2);

            foreach (var fatura in qefGelen)
            {
                if (fatura.Value.durum == 3 && fatura.Value.gonderimDurumu == 4 && fatura.Value.gonderimCevabiKodu == 1200)
                    Entegrasyon.UpdateEfagdnGonderimDurum(fatura.Value.ettn, 3);
                else if (fatura.Value.gonderimCevabiKodu >= 1200 && fatura.Value.gonderimCevabiKodu < 1300)
                    Entegrasyon.UpdateEfagdnGonderimDurum(fatura.Value.ettn, 2);
                else if (fatura.Value.gonderimCevabiKodu == 1300)
                    Entegrasyon.UpdateEfagdnGonderimDurum(fatura.Value.ettn, 3);
                else if (fatura.Value.gonderimDurumu > 2)
                    Entegrasyon.UpdateEfagdnGonderimDurum(fatura.Value.ettn, 0);

                if (fatura.Key.ettn != null)
                {
                    Response.DurumAciklama = fatura.Value.yanitDetayi ?? "";
                    Response.DurumKod = fatura.Value.yanitDurumu == 1 ? "3" : (fatura.Value.yanitDurumu == 2 ? "2" : "0");
                    Response.DurumZaman = DateTime.TryParseExact(fatura.Value.yanitTarihi, "yyyyMMdd", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : new DateTime(1900, 1, 1);
                    Response.UUID = fatura.Key.ettn;

                    Entegrasyon.UpdateEfagdnStatus(Response);
                }
            }
        }

        public override string GonderilenGuncelleByList()
        {
            base.GonderilenGuncelleByList();
            Response = new Results.EFAGDN();
            var qefFatura = new QEF.InvoiceService();
            var qefGelen = qefFatura.GonderilenFaturalar(UUIDs.ToArray());

            //var edmYanitlar = edmFatura.GelenUygulamaYanitlari(day1, day2);

            foreach (var fatura in qefGelen)
            {
                if (fatura.durum == 3 && fatura.gonderimDurumu == 4 && fatura.gonderimCevabiKodu == 1200)
                    Entegrasyon.UpdateEfagdnGonderimDurum(fatura.ettn, 3);
                else if (fatura.gonderimCevabiKodu >= 1200 && fatura.gonderimCevabiKodu < 1300)
                    Entegrasyon.UpdateEfagdnGonderimDurum(fatura.ettn, 2);
                else if (fatura.gonderimCevabiKodu == 1300)
                    Entegrasyon.UpdateEfagdnGonderimDurum(fatura.ettn, 3);
                else
                    Entegrasyon.UpdateEfagdnGonderimDurum(fatura.ettn, 0);

                if (fatura.ettn != null)
                {
                    Response.DurumAciklama = fatura.yanitDetayi ?? "";
                    Response.DurumKod = fatura.yanitDurumu == 1 ? "3" : (fatura.yanitDurumu == 2 ? "2" : "0");
                    Response.DurumZaman = DateTime.TryParseExact(fatura.yanitTarihi, "", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt2) ? dt2 : new DateTime(1900, 1, 1);
                    Response.UUID = fatura.ettn;

                    Entegrasyon.UpdateEfagdnStatus(Response);
                    Entegrasyon.UpdateEfados(qefFatura.FaturaUBLIndir(new[] { fatura.ettn }).First().Value);
                }
            }
            break;
        }


        public override string GelenEsle()
        {
            base.GelenEsle();
            Result = new List<Results.EFAGLN>();
        }



    }
}
