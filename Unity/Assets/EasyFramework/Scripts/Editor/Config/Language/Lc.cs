
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Luban;
using SimpleJSON;

namespace EasyFramework.Edit
{
public partial class Lc
{
    public Languages Languages {get; }

    public Lc(System.Func<string, JSONNode> loader)
    {
        Languages = new Languages(loader("languages"));
        ResolveRef();
    }
    
    private void ResolveRef()
    {
        Languages.ResolveRef(this);
    }
}

}