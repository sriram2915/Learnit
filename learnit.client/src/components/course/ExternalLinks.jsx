import { useEffect, useMemo, useState } from "react";
import {
  FaEdit,
  FaSave,
  FaTimes,
  FaTrash,
  FaExternalLinkAlt,
} from "react-icons/fa";
import styles from "./ExternalLinks.module.css";

const PLATFORMS = [
  "Udemy",
  "Coursera",
  "YouTube",
  "Website",
  "GitHub",
  "Documentation",
];

const ICONS = {
  udemy: "🎓",
  coursera: "📚",
  youtube: "▶️",
  github: "💻",
  documentation: "📖",
  website: "🔗",
};

const isTempId = (id) => typeof id === "string" && id.startsWith("tmp-");

function ExternalLinks({
  links,
  onUpdate,
  onDelete,
  onCreate,
  onDiscardNew,
  autoEditId,
}) {
  const [editing, setEditing] = useState(null);
  const [editingIsNew, setEditingIsNew] = useState(false);
  const [values, setValues] = useState({});
  const [error, setError] = useState("");

  const linkMap = useMemo(() => {
    const map = new Map();
    (links || []).forEach((l) => map.set(String(l.id), l));
    return map;
  }, [links]);

  const startEdit = (link) => {
    setError("");
    setEditing(String(link.id));
    setEditingIsNew(Boolean(link.__isNew) || isTempId(link.id));
    setValues({ platform: link.platform, title: link.title, url: link.url });
  };

  const saveEdit = async () => {
    setError("");

    const platform = (values.platform || "Website").trim();
    const title = (values.title || "").trim();
    const url = (values.url || "").trim();

    if (!url) {
      setError("Please enter a URL");
      return;
    }

    const payload = { platform, title, url };

    try {
      if (editingIsNew) {
        if (typeof onCreate !== "function") {
          setError("Create handler not available");
          return;
        }
        await onCreate(editing, payload);
      } else {
        await onUpdate(editing, payload);
      }

      setEditing(null);
      setEditingIsNew(false);
      setValues({});
    } catch (err) {
      setError(err?.message || "Failed to save link");
    }
  };

  const discardEdit = () => {
    setError("");
    if (editingIsNew) {
      if (typeof onDiscardNew === "function") onDiscardNew(editing);
    }
    setEditing(null);
    setEditingIsNew(false);
    setValues({});
  };

  useEffect(() => {
    if (!autoEditId) return;
    const hit = linkMap.get(String(autoEditId));
    if (!hit) return;
    startEdit(hit);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [autoEditId, linkMap]);

  if (!links || links.length === 0) {
    return <div className={styles.empty}>No external links</div>;
  }

  return (
    <div className={styles.list}>
      {links.map((link) => (
        <div key={link.id} className={styles.item}>
          {editing === String(link.id) ? (
            <div className={styles.form}>
              <select
                value={values.platform}
                onChange={(e) =>
                  setValues({ ...values, platform: e.target.value })
                }
              >
                {PLATFORMS.map((p) => (
                  <option key={p} value={p}>
                    {p}
                  </option>
                ))}
              </select>
              <input
                value={values.title}
                onChange={(e) =>
                  setValues({ ...values, title: e.target.value })
                }
                placeholder="Title"
              />
              <input
                value={values.url}
                onChange={(e) => setValues({ ...values, url: e.target.value })}
                placeholder="https://..."
              />
              {error && <div className={styles.error}>{error}</div>}
              <div className={styles.actions}>
                <button onClick={saveEdit} type="button">
                  <FaSave />
                </button>
                <button onClick={discardEdit} type="button">
                  <FaTimes />
                </button>
              </div>
            </div>
          ) : (
            <>
              <div className={styles.content}>
                <span className={styles.icon}>
                  {ICONS[link.platform.toLowerCase()] || "🔗"}
                </span>
                <div className={styles.info}>
                  <span className={styles.platform}>{link.platform}</span>
                  <span className={styles.title}>
                    {link.title || "Untitled"}
                  </span>
                  <a
                    href={link.url}
                    target="_blank"
                    rel="noopener noreferrer"
                    className={styles.url}
                  >
                    {link.url} <FaExternalLinkAlt />
                  </a>
                </div>
              </div>
              <div className={styles.actions}>
                <button onClick={() => startEdit(link)} type="button">
                  <FaEdit />
                </button>
                <button onClick={() => onDelete(String(link.id))} type="button">
                  <FaTrash />
                </button>
              </div>
            </>
          )}
        </div>
      ))}
    </div>
  );
}

export default ExternalLinks;
