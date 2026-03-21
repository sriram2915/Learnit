import { useState } from "react";
import Modal from "../ui/Modal";
import Button from "../ui/Button";
import Field from "../ui/Field";
import styles from "./JoinClassroomModal.module.css";
import { FaKey } from "react-icons/fa";

function JoinClassroomModal({ isOpen, onClose, onSubmit }) {
  const [inviteCode, setInviteCode] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");

    if (!inviteCode.trim()) {
      setError("Invite code is required");
      return;
    }

    try {
      setLoading(true);
      await onSubmit(inviteCode.trim().toUpperCase());
      setInviteCode("");
    } catch (err) {
      setError(err.message || "Failed to join classroom");
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Join Classroom">
      <form onSubmit={handleSubmit} className={styles.form}>
        <div className={styles.info}>
          <FaKey className={styles.icon} />
          <p>Enter the invite code provided by the classroom creator</p>
        </div>

        {error && <div className={styles.error}>{error}</div>}

        <Field label="Invite Code" required>
          <input
            type="text"
            value={inviteCode}
            onChange={(e) => setInviteCode(e.target.value.toUpperCase())}
            placeholder="e.g., ABC12345"
            maxLength={8}
            autoFocus
            required
            className={styles.input}
          />
        </Field>

        <div className={styles.actions}>
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={loading}>
            Join Classroom
          </Button>
        </div>
      </form>
    </Modal>
  );
}

export default JoinClassroomModal;

