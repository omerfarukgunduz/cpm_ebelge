using System.Data;
using System.Xml.Serialization;
using System;

namespace cpm_ebelge.Models.FIT
{
    public class eFatura
    {
        public override string Gonder()
        {
            base.Gonder();
            var fitEFatura = new FIT.InvoiceWebService();
            SendUBLResponseType[] fitResult;
            if (eFaturaProtectedValues.resend)
                fitResult = fitEFatura.FaturaYenidenGonder(strFatura, createdUBL.UUID.Value, doc.OLDGUID);
            else
                fitResult = fitEFatura.FaturaGonder(strFatura, eFaturaProtectedValues.createdUBL.UUID.Value);

            var envResult = fitEFatura.ZarfDurumSorgula(new[] { fitResult[0].EnvUUID });

            Result.DurumAciklama = envResult[0].Description;
            Result.DurumKod = envResult[0].ResponseCode;
            Result.DurumZaman = envResult[0].IssueDate;
            Result.EvrakNo = fitResult[0].ID;
            Result.UUID = fitResult[0].UUID;
            Result.ZarfUUID = envResult[0].UUID;
            Result.YanitDurum = doc.METHOD == "TEMELFATURA" ? 1 : 0;

            Entegrasyon.UpdateEfagdn(Result, EVRAKSN, null);
            if (Connector.m.DokumanIndir)
            {
                var Gonderilen = fitEFatura.FaturaUBLIndir(new[] { Result.UUID });
                Entegrasyon.UpdateEfados(Gonderilen[0]);
            }
            return "e-Fatura başarıyla gönderildi. \nEvrak No: " + fitResult[0].ID;
        }

        public override string TopluGonder()
        {
            base.TopluGonder();
            var fitFatura = new FIT.InvoiceWebService();

            Dictionary<string, string> strFaturalar = new Dictionary<string, string>();
            Dictionary<string, string> Alici = new Dictionary<string, string>();

            foreach (var Fatura in Faturalar)
            {
                var strFatura = serializer.GetXmlAsString(Fatura.BaseUBL); // XML byte tipinden string tipine dönüştürülür

                strFaturalar.Add(Fatura.BaseUBL.UUID.Value, strFatura);
            }

            var strFats = strFaturalar.ToList();

            int i = 0;
            foreach (var Fatura in Faturalar)
            {
                Connector.m.PkEtiketi = Fatura.PK;
                fitFatura.WebServisAdresDegistir();
                var result = fitFatura.FaturaGonder(strFats[i].Value, strFats[i].Key);

                i++;

                var envResult = fitFatura.ZarfDurumSorgula(new[] { result[0].EnvUUID });

                foreach (var res in result)
                {
                    Result.DurumAciklama = envResult[0].Description;
                    Result.DurumKod = envResult[0].ResponseCode;
                    Result.DurumZaman = envResult[0].IssueDate;
                    Result.EvrakNo = res.ID;
                    Result.UUID = res.UUID;
                    Result.ZarfUUID = res.EnvUUID;
                    Result.YanitDurum = Fatura.METHOD == "TEMELFATURA" ? 1 : 0;
                    Entegrasyon.UpdateEfagdn(Result, Convert.ToInt32(res.CustInvID), null);
                    if (Connector.m.DokumanIndir)
                    {
                        var Gonderilen = fitFatura.FaturaUBLIndir(new[] { Result.UUID });
                        Entegrasyon.UpdateEfados(Gonderilen[0]);
                    }
                }

                sb.AppendLine("e-Fatura başarıyla gönderildi. \nEvrak No: " + result[0].ID);
            }

            return sb.ToString();
        }

        public override string Esle()
        {
            base.Esle();
            var Result = new Results.EFAGDN();
            if (Value.Count > 20)
            {
                throw new Exception("Eşleme işlemi 20 adet üzerinde yapılamamaktadır.\nTarih aralığı verip çalıştır diyerek durum güncellemesi yapabilirsiniz.");
            }
            var fatura = new FIT.InvoiceWebService();
            fatura.WebServisAdresDegistir();

            if (UrlModel.SelectedItem == "FIT")
            {
                var data = Entegrasyon.GetDataFromEvraksn(Value);

                foreach (var d in data)
                {
                    try
                    {
                        //CpmMessageBox.Show("GUID:" + d["ENTEVRAKGUID"].ToString(), "Debug");
                        var yanit = fatura.GelenUygulamaYanitByFatura(d["ENTEVRAKGUID"].ToString(), d["VERGIHESAPNO"].ToString(), d["PKETIKET"].ToString());
                        if (yanit?.Response != null)
                        {
                            if (yanit.Response.Length > 0)
                            {
                                var ynt = yanit.Response.FirstOrDefault(elm => elm.InvoiceUUID == d["ENTEVRAKGUID"].ToString());
                                if (ynt != null && ynt.InvResponses != null)
                                {
                                    if (ynt.InvResponses.Length > 0)
                                    {
                                        //CpmMessageBox.Show(yanit.Response[0].InvResponses[0].ARType + "; " + yanit.Response[0].InvResponses[0].UUID + "; " + yanit.Response[0].InvoiceUUID, "Debug");

                                        Result.UUID = ynt.InvoiceUUID;
                                        Result.DurumAciklama = "";
                                        if (ynt.InvResponses[0].ARNotes != null)
                                            foreach (var note in ynt.InvResponses[0].ARNotes)
                                                Result.DurumAciklama += note + " ";
                                        Result.DurumKod = ynt.InvResponses[0].ARType == "KABUL" ? "2" : "3";
                                        Result.DurumZaman = ynt.InvResponses[0].IssueDate;
                                        Entegrasyon.UpdateEfagdnStatus(Result);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            //else if(!DontShow)
            //	CpmMessageBox.Show("Eşle butonu ile Fatura Yanıtları güncellenmemektedir.\nFatura yanıtlarını tarih aralığı vererek çalıştır butonu yardımıyla güncelleyebilirsiniz!", "Dikkat", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

            var UUIDS = Entegrasyon.GetUUIDFromEvraksn(Value).ToList();

            List<string> UUIDLower = new List<string>();
            bool OK = false;
            Exception exx = null;

            for (int i = 0; i < UUIDS.Count; i++)
            {
                UUIDS[i] = UUIDS[i].ToUpper();
                UUIDLower.Add(UUIDS[i].ToLower());
            }

            try
            {
                var gonderilenler = fatura.GonderilenFaturalar(UUIDLower.ToArray());
                Entegrasyon.UpdateEfagdn(gonderilenler);

                OK = true;
            }
            catch (Exception ex)
            {
                exx = ex;
            }

            try
            {
                if (!OK)
                {
                    var gonderilenler = fatura.GonderilenFaturalar(UUIDS.ToArray());
                    Entegrasyon.UpdateEfagdn(gonderilenler);

                    OK = true;
                }
            }
            catch (Exception ex)
            {
                exx = ex;
            }

            if (!OK && exx != null)
                throw exx;

            break;
        }

        public override string AlinanFaturalarListesi()
        {
            base.AlinanFaturalarListesi();
            var data = new List<AlinanBelge>();
            var fitFatura = new FIT.InvoiceWebService();

            fitFatura.WebServisAdresDegistir();

            for (DateTime dt = StartDate.Date; dt < EndDate.Date.AddDays(1); dt = dt.AddDays(1))
            {
                Connector.m.IssueDate = dt;
                Connector.m.EndDate = dt.AddDays(1);

                var fitResult = fitFatura.GelenFaturalar();

                foreach (var fatura in fitResult)
                {
                    var data.Add(new AlinanBelge
                    {
                        EVRAKGUID = Guid.Parse(fatura.UUID),
                        EVRAKNO = fatura.ID,
                        YUKLEMEZAMAN = fatura.InsertDateTime,
                        GBETIKET = "",
                        GBUNVAN = ""
                    });
                }
            }
            break;
            return data;

        }

        public override string Indir()
        {
            base.Indir();
            var fitFatura = new FIT.InvoiceWebService();
            fitFatura.WebServisAdresDegistir();

            var list = new List<string>();
            List<GetUBLListResponseType> fitGelen = new List<GetUBLListResponseType>();
            for (; day1.Date <= day2.Date; day1 = day1.AddDays(1))
            {
                Connector.m.IssueDate = new DateTime(day1.Year, day1.Month, day1.Day, 0, 0, 0);
                Connector.m.EndDate = new DateTime(day1.Year, day1.Month, day1.Day, 23, 59, 59);

                var tumGelen = fitFatura.GelenFaturalar();
                if (tumGelen.Count() > 0)
                {
                    foreach (var fat in tumGelen)
                    {
                        list.Add(fat.UUID);
                        fitGelen.Add(fat);
                    }
                }
            }

            List<Results.EFAGLN> fitGelenler = new List<Results.EFAGLN>();
            foreach (var f in fitGelen)
            {
                fitGelenler.Add(new Results.EFAGLN
                {
                    DurumAciklama = "",
                    DurumKod = "",
                    DurumZaman = f.InsertDateTime,
                    Etiket = f.Identifier,
                    EvrakNo = f.ID,
                    UUID = f.UUID,
                    VergiHesapNo = f.VKN_TCKN,
                    ZarfUUID = f.EnvUUID.ToString()
                });
            }
            var lists = list.Split(20);

            foreach (var l in lists)
            {
                var ubls = fitFatura.GelenFatura(l.ToArray());
                Entegrasyon.InsertEfagln(ubls, fitGelenler.ToArray());
            }
            break;
        }


        public override string Kabul()
        {
            base.Kabul();
            var adp = new RemoteSqlDataAdapter("SELECT DOSYA FROM EFAGLN WITH (NOLOCK) WHERE EVRAKGUID = @GUID", appConfig.GetConnectionStrings()[0]);
            adp.SelectCommand.Parameters.AddWithValue("@GUID", GUID);

            DataTable dt = new DataTable();
            adp.Fill(ref dt);

            XmlSerializer ser = new XmlSerializer(typeof(UblInvoiceObject.InvoiceType));
            var party = ((UblInvoiceObject.InvoiceType)ser.Deserialize(new MemoryStream((byte[])dt.Rows[0][0]))).AccountingSupplierParty;

            var fitUygulamaYaniti = new FIT.InvoiceWebService();
            fitUygulamaYaniti.WebServisAdresDegistir();
            var fitResult = fitUygulamaYaniti.UygulamaYanitiGonder(GUID, true, Aciklama, party.Party);
            Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { ZarfUUID = fitResult[0].EnvUUID }, GUID, true, Aciklama);
        }

        public override string Red()
        {
            base.Red();
            var adp = new RemoteSqlDataAdapter("SELECT DOSYA FROM EFAGLN WITH (NOLOCK) WHERE EVRAKGUID = @GUID", appConfig.GetConnectionStrings()[0]);
            adp.SelectCommand.Parameters.AddWithValue("@GUID", GUID);

            DataTable dt = new DataTable();
            adp.Fill(ref dt);

            XmlSerializer ser = new XmlSerializer(typeof(UblInvoiceObject.InvoiceType));
            var party = ((UblInvoiceObject.InvoiceType)ser.Deserialize(new MemoryStream((byte[])dt.Rows[0][0]))).AccountingSupplierParty;

            var fitUygulamaYaniti = new FIT.InvoiceWebService();
            fitUygulamaYaniti.WebServisAdresDegistir();
            var fitResult = fitUygulamaYaniti.UygulamaYanitiGonder(GUID, false, Aciklama, party.Party);
            Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { ZarfUUID = fitResult[0].EnvUUID }, GUID, false, Aciklama);
        }

        public override string GonderilenGuncelleByDate()
        {
            base.GonderilenGuncelleByDate();
            var Response = new Results.EFAGDN();

            var fitFatura = new FIT.InvoiceWebService();
            fitFatura.WebServisAdresDegistir();
            List<string> yanitUuid = new List<string>();
            for (; day1.Date <= day2.Date; day1 = day1.AddDays(1))
            {

                Connector.m.IssueDate = new DateTime(day1.Year, day1.Month, day1.Day, 0, 0, 0);
                Connector.m.EndDate = new DateTime(day1.Year, day1.Month, day1.Day, 23, 59, 59);

                var yanit = fitFatura.GelenUygulamaYanitlari();
                var gonderilenler = fitFatura.GonderilenFaturalar();
                //var gonderilenDatalar = fitFatura.FaturaUBLIndir(gonderilenler.Select(elm => elm.UUID).ToArray());

                Entegrasyon.UpdateEfagdn(gonderilenler);

                foreach (var ynt in yanit)
                {
                    yanitUuid.Add(ynt.UUID);
                }
            }
            if (yanitUuid.Count > 0)
            {
                foreach (var yanitUuidPart in yanitUuid.Split(20))
                {
                    var yanit2 = fitFatura.GelenUygulamaYanit(yanitUuidPart.ToArray());

                    foreach (var ynt in yanit2)
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(ApplicationResponseType));
                        var fitResponse = (ApplicationResponseType)serializer.Deserialize(new MemoryStream(ynt));

                        Response.UUID = fitResponse.DocumentResponse[0].DocumentReference.ID.Value;
                        Response.DurumAciklama = "";

                        if (fitResponse.DocumentResponse[0].Response.Description != null)
                            if (fitResponse.DocumentResponse[0].Response.Description.Length > 0)
                                Response.DurumAciklama = fitResponse.DocumentResponse[0].Response.Description[0].Value ?? "";

                        if (fitResponse.Note != null)
                            if (fitResponse.Note.Length > 0)
                                Response.DurumAciklama += ": " + fitResponse.Note[0].Value;

                        Response.DurumKod = fitResponse.DocumentResponse[0].Response.ResponseCode.Value == "KABUL" ? "2" : "3";
                        Response.DurumZaman = fitResponse.IssueDate.Value;

                        Entegrasyon.UpdateEfagdnStatus(Response);
                    }
                }
            }
        }

        public override string GonderilenGuncelleByList()
        {
            base.GonderilenGuncelleByList();
            Response = new Results.EFAGDN();

            var UUID20 = UUIDs.Split(20);
            foreach (var UUID in UUID20)
            {
                var fatura = new FIT.InvoiceWebService();
                fatura.WebServisAdresDegistir();

                byte[][] gonderilenler = null;
                try
                {
                    for (int i = 0; i < UUID.Count; i++)
                        UUID[i] = UUID[i].ToLower();
                    gonderilenler = fatura.FaturaUBLIndir(UUID.ToArray());
                }
                catch { }
                try
                {
                    if (gonderilenler == null)
                    {
                        for (int i = 0; i < UUID.Count; i++)
                            UUID[i] = UUID[i].ToUpper();
                        gonderilenler = fatura.FaturaUBLIndir(UUID.ToArray());
                    }
                }
                catch { }

                foreach (var Gonderilen in gonderilenler)
                    Entegrasyon.UpdateEfados(Gonderilen);
            }
            break;

        }

        public override string GelenEsle()
        {
            base.GelenEsle();
            Result = new List<Results.EFAGLN>();

            var list = new List<string>();
            var fitFatura = new FIT.InvoiceWebService();

            var fitFaturalar = fitFatura.ZarfDurumSorgula2(uuids.ToArray());

            foreach (var f in fitFaturalar)
            {
                var res = new Results.EFAGLN
                {
                    DurumAciklama = f.Description,
                    DurumKod = f.ResponseCode,
                    ZarfUUID = f.UUID
                };
                Entegrasyon.UpdateEfagln(res);
            }
        }


    }
}
