import { useCallback, useEffect, useState } from "react";
import { scheduleApi } from "../services";

const mapDtoToEvent = (dto) => ({
  id: String(dto.id),
  title: dto.title,
  start: dto.startUtc,
  end: dto.endUtc,
  allDay: dto.allDay,
  courseModuleId: dto.courseModuleId,
  courseModule: dto.courseModule,
  backgroundColor: dto.courseModuleId ? "#4CAF50" : undefined,
  borderColor: dto.courseModuleId ? "#4CAF50" : undefined,
});

export function useScheduleData() {
  const [events, setEvents] = useState([]);
  const [availableModules, setAvailableModules] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const loadEvents = useCallback(async () => {
    try {
      setLoading(true);
      setError("");
      const data = await scheduleApi.getScheduleEvents();
      setEvents(data.map(mapDtoToEvent));
    } catch (err) {
      setError(err.message || "Failed to load schedule");
    } finally {
      setLoading(false);
    }
  }, []);

  const loadAvailableModules = useCallback(async () => {
    try {
      const data = await scheduleApi.getAvailableModules();
      setAvailableModules(data);
    } catch (err) {
      console.error("Failed to load available modules", err);
    }
  }, []);

  useEffect(() => {
    loadEvents();
    loadAvailableModules();
  }, [loadEvents, loadAvailableModules]);

  const createEvent = useCallback(async (payload) => {
    const created = await scheduleApi.createScheduleEvent(payload);
    const mapped = mapDtoToEvent(created);
    setEvents((prev) => [...prev, mapped]);
    return mapped;
  }, []);

  const updateEvent = useCallback(async (id, payload) => {
    await scheduleApi.updateScheduleEvent(id, payload);
    setEvents((prev) =>
      prev.map((e) =>
        e.id === String(id)
          ? {
              ...e,
              title: payload.title,
              start: payload.startUtc ?? payload.start,
              end: payload.endUtc ?? payload.end,
              allDay: payload.allDay,
            }
          : e
      )
    );
  }, []);

  const deleteEvent = useCallback(async (id) => {
    await scheduleApi.deleteScheduleEvent(id);
    setEvents((prev) => prev.filter((e) => e.id !== String(id)));
  }, []);

  const linkEventToModule = useCallback(
    async (eventId, moduleId) => {
      await scheduleApi.linkEventToModule(eventId, moduleId);
      await loadEvents();
      await loadAvailableModules();
    },
    [loadEvents, loadAvailableModules]
  );

  const unlinkEventFromModule = useCallback(
    async (eventId) => {
      await scheduleApi.unlinkEventFromModule(eventId);
      await loadEvents();
      await loadAvailableModules();
    },
    [loadEvents, loadAvailableModules]
  );

  const autoSchedule = useCallback(
    async (options = {}) => {
      const result = await scheduleApi.autoScheduleModules(options);
      await loadEvents();
      await loadAvailableModules();
      return result;
    },
    [loadEvents, loadAvailableModules]
  );

  const resetSchedule = useCallback(async () => {
    const result = await scheduleApi.resetSchedule();
    await loadEvents();
    return result;
  }, [loadEvents]);

  return {
    events,
    availableModules,
    loading,
    error,
    setError,
    loadEvents,
    loadAvailableModules,
    createEvent,
    updateEvent,
    deleteEvent,
    linkEventToModule,
    unlinkEventFromModule,
    autoSchedule,
    resetSchedule,
  };
}
