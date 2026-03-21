import { FaFolder, FaRegFile, FaPlus, FaTimes } from "react-icons/fa";
import { v4 as uuidv4 } from "uuid";
import styles from "./ModuleForm.module.css";

function ModuleForm({ modules, setModules }) {
  const updateRoot = (id, field, value) => {
    setModules((prev) =>
      prev.map((m) => (m.id === id ? { ...m, [field]: value } : m))
    );
  };

  const updateSub = (rootId, subId, field, value) => {
    setModules((prev) =>
      prev.map((m) =>
        m.id === rootId
          ? {
              ...m,
              subModules: (m.subModules || []).map((s) =>
                s.id === subId ? { ...s, [field]: value } : s
              ),
            }
          : m
      )
    );
  };

  const addRoot = () => {
    setModules((prev) => [
      ...prev,
      {
        id: window.crypto?.randomUUID ? window.crypto.randomUUID() : uuidv4(),
        title: "",
        duration: "",
        subModules: [],
      },
    ]);
  };

  const addSub = (rootId) => {
    setModules((prev) =>
      prev.map((m) =>
        m.id === rootId
          ? {
              ...m,
              subModules: [
                ...(m.subModules || []),
                {
                  id: window.crypto?.randomUUID
                    ? window.crypto.randomUUID()
                    : uuidv4(),
                  title: "",
                  duration: "",
                },
              ],
            }
          : m
      )
    );
  };

  const removeRoot = (rootId) => {
    setModules((prev) => prev.filter((m) => m.id !== rootId));
  };

  const removeSub = (rootId, subId) => {
    setModules((prev) =>
      prev.map((m) =>
        m.id === rootId
          ? {
              ...m,
              subModules: (m.subModules || []).filter((s) => s.id !== subId),
            }
          : m
      )
    );
  };

  return (
    <div className={styles.section}>
      <div className={styles.header}>
        <div>
          <p className={styles.title}>Course modules *</p>
        </div>
        <button type="button" className={styles.primaryBtn} onClick={addRoot}>
          <FaPlus />
          Add module
        </button>
      </div>

      {modules.length === 0 && (
        <div className={styles.empty}>No modules yet.</div>
      )}

      <ul className={styles.tree}>
        {modules.map((module) => (
          <li className={styles.node} key={module.id}>
            <div className={styles.row} style={{ "--depth": 0 }}>
              <div className={styles.rowLeft}>
                <span className={styles.icon}>
                  <FaFolder />
                </span>
                <div className={styles.inlineForm}>
                  <input
                    type="text"
                    className={`${styles.moduleInput}`}
                    value={module.title}
                    onChange={(e) =>
                      updateRoot(module.id, "title", e.target.value)
                    }
                    placeholder="Module title"
                    required
                  />
                  <input
                    type="number"
                    className={`${styles.hoursInput}`}
                    value={module.duration}
                    onChange={(e) =>
                      updateRoot(module.id, "duration", e.target.value)
                    }
                    placeholder="Hours"
                    min="0"
                    step="1"
                    required
                  />
                </div>
              </div>
              <div className={styles.actions}>
                <button type="button" onClick={() => addSub(module.id)}>
                  <FaPlus />
                  Sub
                </button>
                {modules.length > 1 && (
                  <button
                    type="button"
                    className={styles.danger}
                    onClick={() => removeRoot(module.id)}
                  >
                    <FaTimes />
                  </button>
                )}
              </div>
            </div>

            {(module.subModules || []).length > 0 && (
              <ul className={styles.children}>
                {(module.subModules || []).map((sub) => (
                  <li className={styles.node} key={sub.id}>
                    <div className={styles.row} style={{ "--depth": 1 }}>
                      <div className={styles.rowLeft}>
                        <span className={styles.icon}>
                          <FaRegFile />
                        </span>
                        <div className={styles.inlineForm}>
                          <input
                            type="text"
                            className={styles.moduleInput}
                            value={sub.title}
                            onChange={(e) =>
                              updateSub(
                                module.id,
                                sub.id,
                                "title",
                                e.target.value
                              )
                            }
                            placeholder="Sub-module title"
                            required
                          />
                          <input
                            type="number"
                            className={styles.hoursInput}
                            value={sub.duration}
                            onChange={(e) =>
                              updateSub(
                                module.id,
                                sub.id,
                                "duration",
                                e.target.value
                              )
                            }
                            placeholder="Hours"
                            min="0"
                            step="1"
                            required
                          />
                        </div>
                      </div>
                      <div className={styles.actions}>
                        <button
                          type="button"
                          className={styles.danger}
                          onClick={() => removeSub(module.id, sub.id)}
                        >
                          <FaTimes />
                        </button>
                      </div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </li>
        ))}
      </ul>
    </div>
  );
}

export default ModuleForm;
