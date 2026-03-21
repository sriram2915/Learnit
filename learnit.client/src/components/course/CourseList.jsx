import { useState } from "react";
import { FaSearch, FaSort, FaFilter } from "react-icons/fa";
import { IoIosAdd } from "react-icons/io";
import CourseCard from "./CourseCard";
import Button from "../ui/Button";
import { CourseCardSkeleton } from "../ui/Skeleton";
import styles from "./CourseList.module.css";

function CourseList({
  courses,
  loading,
  onEdit,
  onDelete,
  onNavigate,
  onCreate,
}) {
  const [search, setSearch] = useState("");
  const [sortBy, setSortBy] = useState("createdAt");
  const [sortOrder, setSortOrder] = useState("desc");
  const [selectedFilters, setSelectedFilters] = useState({
    priority: [],
    difficulty: [],
    duration: [],
  });

  const filterGroups = {
    priority: ["Low", "Medium", "High"],
    difficulty: ["Beginner", "Intermediate", "Advanced"],
    duration: ["< 1 hour", "1-3 hours", "> 3 hours"],
  };

  const sortOptions = [
    { value: "createdAt", label: "Date Created" },
    { value: "title", label: "Title" },
    { value: "priority", label: "Priority" },
    { value: "difficulty", label: "Difficulty" },
    { value: "totalEstimatedHours", label: "Total Hours" },
  ];

  const toggleFilter = (group, option) => {
    setSelectedFilters((prev) => {
      const set = new Set(prev[group]);
      set.has(option) ? set.delete(option) : set.add(option);
      return { ...prev, [group]: Array.from(set) };
    });
  };

  const resetFilters = () => {
    setSelectedFilters({ priority: [], difficulty: [], duration: [] });
    setSearch("");
  };

  const getDurationCategory = (hours) => {
    if (hours < 1) return "< 1 hour";
    if (hours <= 3) return "1-3 hours";
    return "> 3 hours";
  };

  const filteredCourses = courses.filter((course) => {
    const matchSearch = course.title
      .toLowerCase()
      .includes(search.toLowerCase());
    const matchPriority =
      selectedFilters.priority.length === 0 ||
      selectedFilters.priority.includes(course.priority);
    const matchDifficulty =
      selectedFilters.difficulty.length === 0 ||
      selectedFilters.difficulty.includes(course.difficulty);
    const matchDuration =
      selectedFilters.duration.length === 0 ||
      selectedFilters.duration.includes(
        getDurationCategory(course.totalEstimatedHours)
      );

    return matchSearch && matchPriority && matchDifficulty && matchDuration;
  });

  const sortedCourses = [...filteredCourses].sort((a, b) => {
    let aVal = a[sortBy];
    let bVal = b[sortBy];

    if (typeof aVal === "string") {
      aVal = aVal.toLowerCase();
      bVal = bVal.toLowerCase();
    }

    const comparison = aVal < bVal ? -1 : aVal > bVal ? 1 : 0;
    return sortOrder === "asc" ? comparison : -comparison;
  });

  return (
    <div className={styles.container}>
      <div className={styles.body}>
        <aside className={styles.filtersPanel}>
          <div className={styles.filtersBar}>
            <span>
              <FaFilter /> Filters
            </span>
            <div className={styles.filterGroups}>
              {Object.entries(filterGroups).map(([group, options]) => (
                <div key={group} className={styles.filterGroup}>
                  <p>{group}</p>
                  <div className={styles.chips}>
                    {options.map((option) => {
                      const active = selectedFilters[group].includes(option);
                      return (
                        <button
                          key={option}
                          type="button"
                          className={`${styles.chip} ${
                            active ? styles.chipActive : ""
                          }`}
                          onClick={() => toggleFilter(group, option)}
                        >
                          {option}
                        </button>
                      );
                    })}
                  </div>
                </div>
              ))}
            </div>
            <button onClick={resetFilters} className={styles.resetBtn}>
              Clear filters
            </button>
          </div>
        </aside>

        <div className={styles.mainColumn}>
          <div className={styles.controls}>
            <div className={styles.searchRow}>
              <div className={styles.searchBox}>
                <FaSearch className={styles.icon} />
                <input
                  type="text"
                  placeholder="Search courses..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </div>

              <Button
                variant="primary"
                onClick={onCreate}
                className={styles.newButton}
              >
                <IoIosAdd size={18} /> New
              </Button>
            </div>

            <div className={styles.metaRow}>
              <div className={styles.sortControls}>
                <FaSort />
                <select
                  value={sortBy}
                  onChange={(e) => setSortBy(e.target.value)}
                >
                  {sortOptions.map((opt) => (
                    <option key={opt.value} value={opt.value}>
                      {opt.label}
                    </option>
                  ))}
                </select>
                <select
                  value={sortOrder}
                  onChange={(e) => setSortOrder(e.target.value)}
                >
                  <option value="asc">Ascending</option>
                  <option value="desc">Descending</option>
                </select>
              </div>

              <span className={styles.count}>
                {loading
                  ? "Loading..."
                  : `${sortedCourses.length} course${
                      sortedCourses.length === 1 ? "" : "s"
                    }`}
              </span>
            </div>
          </div>

          <section className={styles.cardsSection}>
            {loading ? (
              <div className={styles.grid}>
                {Array.from({ length: 6 }).map((_, i) => (
                  <CourseCardSkeleton key={i} />
                ))}
              </div>
            ) : sortedCourses.length === 0 ? (
              <div className={styles.emptyState}>
                <p>No courses found</p>
                {(search ||
                  Object.values(selectedFilters).some((f) => f.length > 0)) && (
                  <button onClick={resetFilters} className={styles.resetBtn}>
                    Clear filters
                  </button>
                )}
              </div>
            ) : (
              <div className={styles.grid}>
                {sortedCourses.map((course) => (
                  <CourseCard
                    key={course.id}
                    course={course}
                    onNavigate={onNavigate}
                    onEdit={onEdit}
                    onDelete={onDelete}
                  />
                ))}
              </div>
            )}
          </section>
        </div>
      </div>
    </div>
  );
}

export default CourseList;
