using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Liv.Lck
{
   public static class LckUpdateManager
   {
      private static readonly HashSet<ILckEarlyUpdate> _earlyUpdates = new HashSet<ILckEarlyUpdate>();
      public static void RegisterEarlyUpdate(ILckEarlyUpdate lckEarlyUpdate) => _earlyUpdates.Add(lckEarlyUpdate);
      public static void UnregisterEarlyUpdate(ILckEarlyUpdate lckEarlyUpdate) => _earlyUpdates.Remove(lckEarlyUpdate);

      private static readonly HashSet<ILckLateUpdate> _lateUpdates = new HashSet<ILckLateUpdate>();
      public static void RegisterLateUpdate(ILckLateUpdate lckLateUpdate) => _lateUpdates.Add(lckLateUpdate);
      public static void UnregisterLateUpdate(ILckLateUpdate lckLateUpdate) => _lateUpdates.Remove(lckLateUpdate);

      [RuntimeInitializeOnLoadMethod]
      private static void Init()
      {
         var currentSystems = PlayerLoop.GetCurrentPlayerLoop();

         var lckEarlyUpdate = new PlayerLoopSystem();
         lckEarlyUpdate.subSystemList = null;
         lckEarlyUpdate.updateDelegate = OnEarlyUpdate;
         lckEarlyUpdate.type = typeof(LckEarlyUpdate);

         var lckLateUpdate = new PlayerLoopSystem();
         lckLateUpdate.subSystemList = null;
         lckLateUpdate.updateDelegate = OnLateUpdate;
         lckLateUpdate.type = typeof(LckLateUpdate);

         var loopWithLckEarlyUpdate = AddSystem<EarlyUpdate>(in currentSystems, lckEarlyUpdate);
         var loopWithLckLateUpdate = AddSystem<PostLateUpdate>(in loopWithLckEarlyUpdate, lckLateUpdate);

         PlayerLoop.SetPlayerLoop(loopWithLckLateUpdate);
      }

      private static PlayerLoopSystem AddSystem<T>(in PlayerLoopSystem loopSystem, PlayerLoopSystem systemToAdd) where T : struct
      {
         var newPlayerLoop = new PlayerLoopSystem();
         newPlayerLoop.loopConditionFunction = loopSystem.loopConditionFunction;
         newPlayerLoop.type = loopSystem.type;
         newPlayerLoop.updateDelegate = loopSystem.updateDelegate;
         newPlayerLoop.updateFunction = loopSystem.updateFunction;

         var newSubSystemList = new List<PlayerLoopSystem>();

         foreach (var subSystem in loopSystem.subSystemList)
         {
            newSubSystemList.Add(subSystem);

            if (subSystem.type == typeof(T))
               newSubSystemList.Add(systemToAdd);
         }

         newPlayerLoop.subSystemList = newSubSystemList.ToArray();
         return newPlayerLoop;
      }
      
      private static void OnEarlyUpdate()
      {
         var earlyUpdateCopy = _earlyUpdates.ToList();
         foreach (var system in earlyUpdateCopy)
         {
            system?.EarlyUpdate();
         }
      }

      private static void OnLateUpdate()
      {
         var lateUpdateCopy = _lateUpdates.ToList();
         foreach (var system in lateUpdateCopy)
         {
            system?.LateUpdate();
         }
      }
   }
}
