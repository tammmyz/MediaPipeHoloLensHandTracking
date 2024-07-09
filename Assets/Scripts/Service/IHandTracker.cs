
using RealityCollective.ServiceFramework.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace HandTracking.Interfaces
{
    public interface IHandTracker : IService
    {
        Task<List<int>> DetectPalm(Texture2D texture);
    }
}