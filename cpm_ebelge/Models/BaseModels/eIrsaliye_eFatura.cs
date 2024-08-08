namespace cpm_ebelge.Models.BaseModels
{
    public abstract class eIrsaliye_eFatura : BaseDocument
    {
        public abstract List<AlinanBelge> AlinanFaturalarListesi();
        public abstract void Indir();
        public abstract void Kabul();
        public abstract void Red();
        public abstract void GonderilenGuncelleByDate();
        public abstract void GonderilenGuncelleByList();
    }
    
    }

