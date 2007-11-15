using System;
using System.Collections.Generic;
using System.Text;

namespace FuseSolution.Tracker {
  public interface ITrackerFactory {
    Tracker MakeTracker();
  }

  class SimpleTrackerFactory {
    Tracker MakeTracker() {
      Tracker tracker = new Tracker();
      tracker.TrackerType = TrackerType.Simple;
      return tracker;
    }
  }
  
  class DhtTrackerFactory {
    Tracker MakeTracker() {
      Tracker tracker = new Tracker();
      tracker.TrackerType = TrackerType.Dht;
      return tracker;
    }
  }

  class LocalTrackerFactory {
    Tracker MakeTracker() {
      Tracker tracker = new Tracker();
      tracker.TrackerType = TrackerType.Local;
      return tracker;
    }
  }
}
