import { useCallback, useEffect, useState } from "react";
import { progressApi } from "../services";

export function useProgressDashboard() {
  const [dashboard, setDashboard] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError("");
      const data = await progressApi.getProgressDashboard();
      setDashboard(data);
      return data;
    } catch (err) {
      setDashboard(null);
      setError(err?.message || "Failed to load progress");
      return null;
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  return {
    dashboard,
    stats: dashboard?.stats,
    loading,
    error,
    reload: load,
  };
}
