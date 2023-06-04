// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/10/03

namespace DG.Tweening.Timeline.Core.Plugins
{
    public interface IPlugin
    {
        IPluginData[] editor_iPluginDatas { get; } // Used for operations in the Editor where conversion will happen
        int totPluginDatas { get; }
    }
}