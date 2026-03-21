import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { classroomApi } from "../../services";
import { Loading, ErrorMessage, ToastContainer, useToast } from "../ui/index";
import CreateClassroomModal from "./CreateClassroomModal";
import JoinClassroomModal from "./JoinClassroomModal";
import styles from "./ClassroomList.module.css";
import { FaUsers, FaBook, FaPlus, FaSignInAlt, FaSearch } from "react-icons/fa";

function ClassroomList() {
  const navigate = useNavigate();
  const toast = useToast();
  const [classrooms, setClassrooms] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showJoinModal, setShowJoinModal] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");

  useEffect(() => {
    fetchClassrooms();
  }, []);

  const fetchClassrooms = async () => {
    try {
      setLoading(true);
      setError("");
      const data = await classroomApi.getClassrooms();
      setClassrooms(data);
    } catch (err) {
      setError(err.message || "Failed to load classrooms");
    } finally {
      setLoading(false);
    }
  };

  const handleCreateClassroom = async (classroomData) => {
    try {
      const classroom = await classroomApi.createClassroom(classroomData);
      setShowCreateModal(false);
      toast.success("Classroom created successfully!");
      await fetchClassrooms();
      return classroom; // Return classroom so modal can use it for course sharing
    } catch (err) {
      toast.error(err.message || "Failed to create classroom");
      throw err;
    }
  };

  const handleJoinClassroom = async (inviteCode) => {
    try {
      await classroomApi.joinClassroom(inviteCode);
      setShowJoinModal(false);
      toast.success("Successfully joined classroom!");
      await fetchClassrooms();
    } catch (err) {
      toast.error(err.message || "Failed to join classroom");
      throw err;
    }
  };

  const handleDeleteClassroom = async (id) => {
    if (!confirm("Are you sure you want to delete this classroom? This action cannot be undone.")) {
      return;
    }

    try {
      await classroomApi.deleteClassroom(id);
      toast.success("Classroom deleted successfully");
      await fetchClassrooms();
    } catch (err) {
      toast.error(err.message || "Failed to delete classroom");
    }
  };

  const filteredClassrooms = classrooms.filter((classroom) =>
    classroom.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    classroom.description.toLowerCase().includes(searchQuery.toLowerCase())
  );

  if (loading) {
    return <Loading />;
  }

  return (
    <div className={styles.classroomList}>
      <div className={styles.header}>
        <div className={styles.headerTop}>
          <h1>Community Classrooms</h1>
          <div className={styles.actions}>
            <button
              className={styles.joinButton}
              onClick={() => setShowJoinModal(true)}
            >
              <FaSignInAlt /> Join Classroom
            </button>
            <button
              className={styles.createButton}
              onClick={() => setShowCreateModal(true)}
            >
              <FaPlus /> Create Classroom
            </button>
          </div>
        </div>

        <div className={styles.searchBar}>
          <FaSearch className={styles.searchIcon} />
          <input
            type="text"
            placeholder="Search classrooms..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className={styles.searchInput}
          />
        </div>
      </div>

      {error && <ErrorMessage message={error} />}

      {filteredClassrooms.length === 0 ? (
        <div className={styles.emptyState}>
          {classrooms.length === 0 ? (
            <>
              <FaUsers className={styles.emptyIcon} />
              <h2>No Classrooms Yet</h2>
              <p>Create your first classroom or join an existing one to get started!</p>
              <button
                className={styles.createButton}
                onClick={() => setShowCreateModal(true)}
              >
                <FaPlus /> Create Your First Classroom
              </button>
            </>
          ) : (
            <>
              <FaSearch className={styles.emptyIcon} />
              <h2>No Classrooms Found</h2>
              <p>Try adjusting your search query.</p>
            </>
          )}
        </div>
      ) : (
        <div className={styles.classroomGrid}>
          {filteredClassrooms.map((classroom) => (
            <div
              key={classroom.id}
              className={styles.classroomCard}
              onClick={() => navigate(`/app/classrooms/${classroom.id}`)}
            >
              <div className={styles.cardHeader}>
                <h3>{classroom.name}</h3>
                {classroom.isCreator && (
                  <span className={styles.creatorBadge}>Creator</span>
                )}
                {classroom.userRole !== "Member" && !classroom.isCreator && (
                  <span className={styles.roleBadge}>{classroom.userRole}</span>
                )}
              </div>
              <p className={styles.description}>{classroom.description || "No description"}</p>
              <div className={styles.cardStats}>
                <div className={styles.stat}>
                  <FaUsers />
                  <span>{classroom.memberCount} {classroom.memberCount === 1 ? "Member" : "Members"}</span>
                </div>
                <div className={styles.stat}>
                  <FaBook />
                  <span>{classroom.courseCount} {classroom.courseCount === 1 ? "Course" : "Courses"}</span>
                </div>
              </div>
              <div className={styles.cardFooter}>
                <div className={styles.inviteCode}>
                  <span className={styles.codeLabel}>Code:</span>
                  <span className={styles.codeValue}>{classroom.inviteCode}</span>
                </div>
                {classroom.isCreator && (
                  <button
                    className={styles.deleteButton}
                    onClick={(e) => {
                      e.stopPropagation();
                      handleDeleteClassroom(classroom.id);
                    }}
                  >
                    Delete
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      <CreateClassroomModal
        isOpen={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        onSubmit={handleCreateClassroom}
      />

      <JoinClassroomModal
        isOpen={showJoinModal}
        onClose={() => setShowJoinModal(false)}
        onSubmit={handleJoinClassroom}
      />

      <ToastContainer toasts={toast.toasts} removeToast={toast.removeToast} />
    </div>
  );
}

export default ClassroomList;

