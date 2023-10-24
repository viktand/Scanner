namespace KernelWebApi
{
    public interface IKernelRepository
    {
        bool ButtonCheck();
        bool ButtonIni();
        bool PaperSwitch(bool state, out string message);
        string PowerSwitch();
        string ScannSwitch();
        void Start(string port);
        bool Stop();

        public bool Press { get; set; }
    }
}