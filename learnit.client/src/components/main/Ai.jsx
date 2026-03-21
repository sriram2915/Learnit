import { useEffect, useMemo, useState } from "react";
import { aiApi } from "../../services";
import { AiTabs } from "./ai/AiTabs";
import { ChatPanel } from "./ai/ChatPanel";
import { ComparePanel } from "./ai/ComparePanel";
import styles from "./Ai.module.css";

function Ai() {
  const [messages, setMessages] = useState([
    {
      role: "assistant",
      content: "Hey! Ask me about courses, scheduling, or progress.",
    },
  ]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [insights, setInsights] = useState([]);
  const [friends, setFriends] = useState([]);
  const [selectedFriendIds, setSelectedFriendIds] = useState([]);
  const [activeTab, setActiveTab] = useState("chat");

  useEffect(() => {
    loadFriends();
  }, []);

  useEffect(() => {
    setSelectedFriendIds((prev) =>
      prev.filter((id) => friends.some((f) => f.id === id))
    );
  }, [friends]);

  const userHistory = useMemo(
    () =>
      messages
        .filter((m) => m.role !== "assistant")
        .map((m) => ({ role: "user", content: m.content })),
    [messages]
  );

  const sendMessage = async (text) => {
    if (!text.trim()) return;
    setMessages((prev) => [...prev, { role: "user", content: text }]);
    setInput("");
    setLoading(true);
    try {
      const reply = await aiApi.chat({ message: text, history: userHistory });
      setMessages((prev) => [
        ...prev,
        { role: "assistant", content: reply.reply || "I couldn't generate a response. Please try again." },
      ]);
    } catch (err) {
      console.error("[AI Chat] Error:", err);
      const errorMessage = err.response?.data?.message 
        || err.message 
        || "Sorry, I'm having trouble connecting. Please check your connection and try again.";
      setMessages((prev) => [
        ...prev,
        { role: "assistant", content: `❌ ${errorMessage}` },
      ]);
    } finally {
      setLoading(false);
    }
  };

  const loadFriends = async () => {
    try {
      const data = await aiApi.listFriends();
      setFriends(data);
    } catch (err) {
      // silent
    }
  };

  const compareFriends = async () => {
    if (!selectedFriendIds.length) return;
    setLoading(true);
    try {
      const data = await aiApi.compareFriends(selectedFriendIds);
      setInsights(data.insights || []);
    } catch (err) {
      setInsights([
        { title: "Error", detail: err.message || "Compare failed" },
      ]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <section className={styles.page}>
      <AiTabs activeTab={activeTab} onChange={setActiveTab} />

      {activeTab === "chat" && (
        <ChatPanel
          messages={messages}
          loading={loading}
          input={input}
          onInputChange={setInput}
          onSend={() => sendMessage(input)}
          onQuickSend={sendMessage}
        />
      )}

      {activeTab === "compare" && (
        <ComparePanel
          friends={friends}
          selectedFriendIds={selectedFriendIds}
          insights={insights}
          loading={loading}
          onSelectFriend={(id) => setSelectedFriendIds([id])}
          onCompare={compareFriends}
        />
      )}
    </section>
  );
}

export default Ai;
