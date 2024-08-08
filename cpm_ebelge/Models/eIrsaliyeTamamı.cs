namespace cpm_ebelge.Models
{

    public class eIrsaliyeProtectedValues
    {
        public dynamic strFatura { get; set; }
        public dynamic doc { get; set; }

    }
    public abstract class EIrsaliye : EIrsaliye_EFatura
    {
        protected eIrsaliyeProtectedValues eIrsaliyeProtectedValues { get; set; } = new eIrsaliyeProtectedValues();

        public static string TopluGonder(string PK, List<DespatchAdviceType> Irsaliyeler, string ENTSABLON)
        {
            {
                StringBuilder sb = new StringBuilder();
                var Result = new Results.EFAGDN();

                if (Connector.m.SchematronKontrol)
                {
                    DespatchAdviceSerializer ser = new DespatchAdviceSerializer();
                    foreach (var Irsaliye in Irsaliyeler)
                    {
                        var schematronResult = SchematronChecker.Check(Irsaliye, SchematronDocType.eIrsaliye);
                        if (schematronResult.SchemaResult != "Başarılı" || schematronResult.SchematronResult != "Başarılı")
                            throw new Exception(schematronResult.Detail);
                    }
                }

            }


            ///TOPLUGONDER---SWITCHE KADAR OLAN KISIM İÇİNDE OLUCAK DİĞERLERİ SEALED CLASSTA-----------------------------------------------------------------------------------------------------

        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            

        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }

        public static string Gonder(int EVRAKSN)
        {
            DespatchAdvice despatchAdvice = new DespatchAdvice();
            eIrsaliyeProtectedValues.doc = despatchAdvice.CreateDespactAdvice(EVRAKSN);

            if (UrlModel.SelectedItem == "QEF" && string.IsNullOrEmpty(eIrsaliyeProtectedValues.doc.ID.Value) && Connector.m.SablonTip)
            {
                var sablon = string.IsNullOrEmpty(despatchAdvice.ENTSABLON) ? Connector.m.Sablon : despatchAdvice.ENTSABLON;
                var notes = eIrsaliyeProtectedValues.doc.Note.ToList();
                notes.Add(new UblDespatchAdvice.NoteType { Value = $"#EFN_SERINO_TERCIHI#{sablon}#" });

                eIrsaliyeProtectedValues.doc.Note = notes.ToArray();
            }

            if (Connector.m.SchematronKontrol)
            {
                schematronResult = SchematronChecker.Check(eIrsaliyeProtectedValues.doc, SchematronDocType.eIrsaliye);
                if (schematronResult.SchemaResult != "Başarılı" || schematronResult.SchematronResult != "Başarılı")
                    throw new Exception(schematronResult.Detail);
            }

            UBLBaseSerializer serializer = new DespatchAdviceSerializer();
            eIrsaliyeProtectedValues.strFatura = serializer.GetXmlAsString(eIrsaliyeProtectedValues.doc);

            Connector.m.PkEtiketi = despatchAdvice.PK;
            Entegrasyon.EfagdnUuid(EVRAKSN, eIrsaliyeProtectedValues.doc.UUID.Value);


            var Result = new Results.EFAGDN();

        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }
        public static void Esle(string GUID)
        {

        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }
        public static void Esle(int EVRAKSN, DateTime GonderimTarih, string EVRAKGUID)

        {

        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }
        public static List<AlinanBelge> AlinanFaturalarListesi(DateTime StartDate, DateTime EndDate)
        {

        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
           
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }
        public static void Indir(DateTime day1, DateTime day2)
        {
        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }

        public static void Kabul(string GUID, string Aciklama)
        {

        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
           
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }
        public static void Red(string GUID, string Aciklama)
        {
        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
           
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }
        public static void GonderilenGuncelleByDate(DateTime start, DateTime end)
        {
        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }

        public static void GonderilenGuncelle(List<string> UUIDs)
        {
        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
            
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
           
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
            
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            
        }
        /// SADECE EIRSALIYEDE OLANLAR 
        public static void YanitGonder(ReceiptAdviceType Yanit)
        {
            ReceiptAdviceSerializer serializer = new ReceiptAdviceSerializer();
            var docStr = serializer.GetXmlAsString(Yanit);
            eIrsaliyeProtectedValues.doc = Encoding.UTF8.GetBytes(docStr);
            switch (UrlModel.SelectedItem)
            {
                case "FIT":
                case "ING":
                case "INGBANK":
                    var fitIrsaliye = new FIT.DespatchWebService();
                    fitIrsaliye.WebServisAdresDegistir();

                    //var res = fitIrsaliye.IrsaliyeYanitiGonder(Yanit);
                    //var fitDosya = fitIrsaliye.IrsaliyeYanitiIndir(res.Response[0].UUID);
                    //var fitYnt = Entegrasyon.ConvertToYanit(fitDosya.Response[0], "GDN");
                    //
                    //Entegrasyon.InsertIntoEirYnt(fitYnt);

                    Yanit.ID = new UblReceiptAdvice.IDType { Value = Entegrasyon.GetIrsaliyeYanitEvrakNo() };

                    var res = fitIrsaliye.IrsaliyeYanitiGonder(Yanit);
                    var fitDosya = fitIrsaliye.GonderilenIrsaliyeYanitlari(res.Response[0].EnvUUID, res.Response[0].UUID);
                    var fitYnt = Entegrasyon.ConvertToYanit(fitDosya, "GDN", res.Response[0].EnvUUID);
                    Entegrasyon.InsertIntoEirYnt(fitYnt);
                    break;
                case "DPLANET":
                    var dpIrsaliye = new DigitalPlanet.DespatchWebService();
                    dpIrsaliye.WebServisAdresDegistir();
                    dpIrsaliye.Login();

                    var dpSonuc = dpIrsaliye.EIrsaliyeCevap(eIrsaliyeProtectedValues.doc);
                    //File.WriteAllText("ReceiptAdvice_Resp.json", JsonConvert.SerializeObject(dpSonuc));
                    if (dpSonuc.ServiceResult == COMMON.dpDespatch.Result.Error)
                        throw new Exception(dpSonuc.ServiceResultDescription);

                    foreach (var receipments in dpSonuc.Receipments)
                    {
                        var irsaliyeYanit = dpIrsaliye.GidenEIrsaliyeYanitIndir(receipments.UUID);
                        var ynt = Entegrasyon.ConvertToYanit(irsaliyeYanit.Receipments[0], "GDN");

                        Entegrasyon.InsertIntoEirYnt(ynt);
                    }
                    break;
                case "EDM":
                    break;
                case "QEF":
                    break;
                default:
                    throw new Exception("Tanımlı Entegratör Bulunamadı!");
            }
        }
        public static void YanitGuncelle(string UUID, string ZarfGuid)
        {
            switch (UrlModel.SelectedItem)
            {
                case "FIT":
                case "ING":
                case "INGBANK":
                    var fitIrsaliye = new FIT.DespatchWebService();
                    fitIrsaliye.WebServisAdresDegistir();
                    var fitDosya = fitIrsaliye.GonderilenIrsaliyeYanitlari(ZarfGuid, UUID);
                    var fitYnt = Entegrasyon.ConvertToYanit(fitDosya, "GDN", ZarfGuid);

                    if (appConfig.Debugging)
                    {
                        MessageBox.Show(ZarfGuid);
                    }
                    Entegrasyon.UpdateEirYnt(fitYnt);
                    break;
                case "DPLANET":
                    var dpIrsaliye = new DigitalPlanet.DespatchWebService();
                    dpIrsaliye.WebServisAdresDegistir();
                    dpIrsaliye.Login();

                    var dpSonuc = dpIrsaliye.EIrsaliyeYanitDurumu(UUID);
                    var durum = new EIRYNT
                    {
                        DURUMACIKLAMA = dpSonuc.StatusDescription,
                        DURUMKOD = dpSonuc.StatusCode == 54 ? "1300" : dpSonuc.StatusCode + "",
                        EVRAKGUID = UUID
                    };

                    Entegrasyon.UpdateEirYnt(durum);
                    break;
                case "EDM":
                    break;
                default:
                    throw new Exception("Tanımlı Entegratör Bulunamadı!");
            }
        }
        public static void GonderilenYanitlar(string UUID)
        {
            switch (UrlModel.SelectedItem)
            {
                case "ING":
                case "INGBANK":
                case "FIT":
                    var fitIrsaliye = new FIT.DespatchWebService();
                    fitIrsaliye.WebServisAdresDegistir();
                    var yanitlar = fitIrsaliye.IrsaliyeYanitiIndir(UUID);

                    var yanitlar2 = Entegrasyon.ConvertToYanitList(yanitlar.Response[0], "GDN", Entegrasyon.GetEvrakNoFromGuid(UUID));
                    if (appConfig.Debugging)
                    {
                        if (yanitlar?.Response?.Length > 0)
                        {
                            if (yanitlar?.Response[0]?.Receipts?.Length > 0)
                            {
                                MessageBox.Show(yanitlar.Response[0].Receipts[0].EnvUUID);
                            }
                        }
                    }
                    foreach (var yanit in yanitlar2)
                    {
                        Entegrasyon.InsertIntoEirYnt(yanit);
                        Entegrasyon.UpdateEirYnt(yanit);
                    }
                    break;
                case "DPLANET":
                    //var dpIrsaliye = new DigitalPlanet.DespatchWebService();
                    //dpIrsaliye.EIrsaliyeGonderilenYanitlar(UUID);
                    break;
                case "EDM":
                    break;
                default:
                    throw new Exception("Tanımlı Entegratör Bulunamadı!");
            }
        }
    } 
}
