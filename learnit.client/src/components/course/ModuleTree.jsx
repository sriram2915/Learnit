import { useMemo, useState } from "react";
import {
  FaEdit,
  FaPlus,
  FaSave,
  FaStickyNote,
  FaTimes,
  FaFolder,
  FaRegFile,
  FaUndo,
} from "react-icons/fa";
import styles from "./ModuleTree.module.css";

const emptyModule = {
  title: "",
  estimatedHours: "",
  description: "",
  notes: "",
};

function ModuleTree({
  modules = [],
  isExternalCourse = false,
  isQuizEnabled = false,
  onUpdate,
  onToggleCompletion,
  onResetCompletion,
  onAdd,
}) {
  const [editingId, setEditingId] = useState(null);
  const [formValues, setFormValues] = useState(emptyModule);
  const [addTarget, setAddTarget] = useState(undefined);
  const [addValues, setAddValues] = useState(emptyModule);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");

  const sortByOrder = (list) =>
    [...list].sort((a, b) => (a.order ?? 0) - (b.order ?? 0));

  const roots = useMemo(
    () => sortByOrder(modules.filter((m) => !m.parentModuleId)),
    [modules]
  );

  const childrenMap = useMemo(() => {
    const grouped = modules
      .filter((m) => m.parentModuleId)
      .reduce((acc, m) => {
        acc[m.parentModuleId] = acc[m.parentModuleId] || [];
        acc[m.parentModuleId].push(m);
        return acc;
      }, {});

    Object.keys(grouped).forEach((key) => {
      grouped[key] = sortByOrder(grouped[key]);
    });

    return grouped;
  }, [modules]);

  const startAdd = (parentId = null) => {
    setAddTarget(parentId);
    setError("");
    setEditingId(null);
    setAddValues(emptyModule);
  };

  const handleAdd = async () => {
    if (!addValues.title.trim()) {
      setError("Module title is required");
      return;
    }

    setPending(true);
    setError("");
    try {
      await onAdd({
        title: addValues.title.trim(),
        estimatedHours: addValues.estimatedHours
          ? parseInt(addValues.estimatedHours, 10)
          : 0,
        description: addValues.description?.trim() || "",
        notes: addValues.notes?.trim() || "",
        parentModuleId: addTarget ?? null,
      });
      setAddTarget(undefined);
      setAddValues(emptyModule);
    } catch (err) {
      setError(err?.message || "Failed to add module");
    } finally {
      setPending(false);
    }
  };

  const startEdit = (module) => {
    setEditingId(module.id);
    setAddTarget(undefined);
    setError("");
    setFormValues({
      title: module.title,
      estimatedHours: module.estimatedHours ?? "",
      description: module.description || "",
      notes: module.notes || "",
    });
  };

  const handleSave = async () => {
    if (!editingId) return;
    setPending(true);
    setError("");
    try {
      await onUpdate(editingId, {
        title: formValues.title,
        description: formValues.description,
        estimatedHours: formValues.estimatedHours
          ? parseInt(formValues.estimatedHours, 10)
          : 0,
        notes: formValues.notes,
      });
      setEditingId(null);
      setFormValues(emptyModule);
    } catch (err) {
      setError(err?.message || "Failed to update module");
    } finally {
      setPending(false);
    }
  };

  const cancelEditing = () => {
    setEditingId(null);
    setFormValues(emptyModule);
  };

  const cancelAdd = () => {
    setAddTarget(undefined);
    setAddValues(emptyModule);
    setError("");
  };

  const renderAddRow = (parentId, depth = 0) => (
    <div className={styles.addRow} style={{ "--depth": depth }}>
      <div className={styles.rowInputs}>
        <input
          value={addValues.title}
          onChange={(e) =>
            setAddValues({ ...addValues, title: e.target.value })
          }
          placeholder={parentId ? "New sub-module" : "New module"}
          autoFocus
        />
        <input
          type="number"
          value={addValues.estimatedHours}
          onChange={(e) =>
            setAddValues({ ...addValues, estimatedHours: e.target.value })
          }
          placeholder="Hours"
          min="0"
          step="0.5"
        />
        <div className={styles.addActions}>
          <button type="button" onClick={handleAdd} disabled={pending}>
            <FaSave />
          </button>
          <button type="button" onClick={cancelAdd}>
            <FaTimes />
          </button>
        </div>
      </div>
      <textarea
        value={addValues.description}
        onChange={(e) =>
          setAddValues({ ...addValues, description: e.target.value })
        }
        placeholder="Description (optional)"
        rows={2}
      />
    </div>
  );

  const renderNode = (node, depth = 0, visited = new Set()) => {
    // Defensive: Prevent cycles, self-parenting, and runaway depth
    if (
      !node ||
      visited.has(node.id) ||
      depth > 5 ||
      node.parentModuleId === node.id
    ) {
      // Prevent cycles, self-parenting, or runaway depth from malformed parent links
      return null;
    }

    const nextVisited = new Set(visited);
    nextVisited.add(node.id);

    const children = childrenMap[node.id] || [];
    const isEditing = editingId === node.id;
    const isAddingHere = addTarget === node.id;
    const rowClass = `${styles.row} ${
      node.isCompleted ? styles.completed : ""
    }`;

    const handleToggle = () => {
      // When quizzes are enabled, only send the toggled module id so the
      // parent component can open the quiz modal and block completion.
      const includeChildren = depth === 0 && !isQuizEnabled;
      const childIds = includeChildren ? children.map((c) => c.id) : [];
      const targetState = !node.isCompleted;
      onToggleCompletion([node.id, ...childIds], targetState);
    };

    const handleReset = async () => {
      if (!onResetCompletion) return;

      setPending(true);
      setError("");
      try {
        await onResetCompletion(node.id);
      } catch (err) {
        setError(err?.message || "Failed to reset module completion");
      } finally {
        setPending(false);
      }
    };

    return (
      <li className={styles.node} key={node.id}>
        <div className={rowClass} style={{ "--depth": depth }}>
          <div className={styles.rowLeft}>
            <label className={styles.checkbox}>
              <input
                type="checkbox"
                checked={!!node.isCompleted}
                onChange={handleToggle}
              />
              <span className={styles.checkVisual} aria-hidden="true" />
            </label>
            <span className={styles.icon}>
              {depth === 0 ? <FaFolder /> : <FaRegFile />}
            </span>
            {isEditing ? (
              <div className={styles.inlineForm}>
                <input
                  value={formValues.title}
                  onChange={(e) =>
                    setFormValues({ ...formValues, title: e.target.value })
                  }
                  placeholder="Title"
                  autoFocus
                />
                <input
                  type="number"
                  value={formValues.estimatedHours}
                  onChange={(e) =>
                    setFormValues({
                      ...formValues,
                      estimatedHours: e.target.value,
                    })
                  }
                  placeholder="Hours"
                  min="0"
                  step="0.5"
                />
              </div>
            ) : (
              <div className={styles.labelBlock}>
                <span className={styles.title}>{node.title}</span>
                {node.description && (
                  <span className={styles.muted}>{node.description}</span>
                )}
              </div>
            )}
          </div>
          <div className={styles.actions}>
            <span className={styles.badge}>{node.estimatedHours ?? 0}h</span>
            {isEditing ? (
              <>
                <button
                  type="button"
                  onClick={handleSave}
                  disabled={pending}
                  title="Save"
                >
                  <FaSave />
                </button>
                <button type="button" onClick={cancelEditing} title="Cancel">
                  <FaTimes />
                </button>
              </>
            ) : (
              <>
                {isExternalCourse && node.isCompleted && (
                  <button
                    type="button"
                    onClick={handleReset}
                    className={styles.iconBtn}
                    disabled={pending}
                    title="Reset completion (retake quiz)"
                  >
                    <FaUndo />
                  </button>
                )}
                <button
                  type="button"
                  onClick={() => startEdit(node)}
                  className={styles.iconBtn}
                  title="Edit"
                >
                  <FaEdit />
                </button>
                {depth === 0 && (
                  <button
                    type="button"
                    onClick={() => startAdd(node.id)}
                    className={styles.iconBtn}
                    title="Add"
                  >
                    <FaPlus />
                  </button>
                )}
              </>
            )}
          </div>
        </div>

        {isEditing && (
          <div className={styles.editNotes} style={{ "--depth": depth }}>
            <textarea
              value={formValues.description}
              onChange={(e) =>
                setFormValues({ ...formValues, description: e.target.value })
              }
              placeholder="Description"
              rows={2}
            />
            <textarea
              value={formValues.notes}
              onChange={(e) =>
                setFormValues({ ...formValues, notes: e.target.value })
              }
              placeholder="Notes"
              rows={2}
            />
          </div>
        )}

        {isAddingHere && renderAddRow(node.id, depth + 1)}

        {children.length > 0 && (
          <ul className={styles.children}>
            {children.map((child) => renderNode(child, depth + 1, nextVisited))}
          </ul>
        )}
      </li>
    );
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <p className={styles.title}>Course Modules</p>
        <button
          type="button"
          className={styles.primaryBtn}
          onClick={() => startAdd(null)}
        >
          <FaPlus />
        </button>
      </div>

      {error && <div className={styles.error}>{error}</div>}

      {addTarget === null && renderAddRow(null, 0)}

      {roots.length === 0 ? (
        <div className={styles.empty}>No modules yet.</div>
      ) : (
        <ul className={styles.tree}>{roots.map((root) => renderNode(root))}</ul>
      )}
    </div>
  );
}

export default ModuleTree;
