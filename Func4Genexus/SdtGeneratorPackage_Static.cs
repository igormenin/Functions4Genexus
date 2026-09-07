using System;
using System.IO;

namespace Func4Genexus
{
    public partial class SdtGeneratorPackage
    {
        static SdtGeneratorPackage()
        {
            try {
                File.AppendAllText(@"C:\Projetos\Func4Genexus\log.txt", "SdtGeneratorPackage static constructor executed!\n");
            } catch {}
        }
    }
}
