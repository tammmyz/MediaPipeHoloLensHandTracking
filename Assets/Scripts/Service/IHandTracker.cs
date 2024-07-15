
using RealityCollective.ServiceFramework.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace HandTracking.Interfaces
{
    public interface IHandTracker : IService
    {
        Texture2D preprocess(Texture2D texture);
        Task<int> DetectPalms(Texture2D texture);
    }
}