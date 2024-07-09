
using RealityCollective.ServiceFramework.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Dnn.Interfaces
{
    public interface IProcessor : IService
    {
        Task<List<int>> DetectPalm(Texture2D texture);
    }
}