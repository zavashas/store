(function () {
    if (window.__consultantInited) return;
    window.__consultantInited = true;

    const $ = (id) => document.getElementById(id);
    const root = $("mgr-root");
    const cfg = {
        hubUrl: root?.dataset?.hubUrl || "/hubs/chat",
        apiBase: (root?.dataset?.apiBase || "").replace(/\/$/, ""), 
        agentId: parseInt(root?.dataset?.agentId || "0", 10),
        isAdmin: (root?.dataset?.isAdmin || "false") === "True" || (root?.dataset?.isAdmin || "false") === "true"
    };

    const queueList = $("queueList");
    const activeList = $("activeList");
    const closedList = $("closedList"); 
    const refreshQueueBtn = $("refreshQueue");
    const chatTitle = $("chatTitle");
    const chatBox = $("chatBox");
    const msgInput = $("msgInput");
    const sendBtn = $("sendBtn");
    const closeChatBtn = $("closeChatBtn");
    const status = $("status");

    let connection;
    let currentChatId = null;

    function setStatus(text) { if (status) status.innerText = text || ""; }

    function renderMessage({ senderRole, body, createdAt }) {
        const wrap = document.createElement("div");
        wrap.style.marginBottom = "8px";

        const bubble = document.createElement("div");

        const mine = senderRole === "consultant" || senderRole === "admin";
        bubble.style.maxWidth = "80%";
        bubble.style.padding = "8px 10px";
        bubble.style.borderRadius = "10px";
        bubble.style.background = mine ? "#d1e7dd" : "#fff";
        bubble.style.border = "1px solid #e5e5e5";
        bubble.style.display = "inline-block";
        bubble.innerText = body ?? "";

        const row = document.createElement("div");
        row.style.display = "flex";
        row.style.justifyContent = mine ? "flex-end" : "flex-start";
        row.appendChild(bubble);

        const meta = document.createElement("div");
        meta.style.fontSize = "11px";
        meta.style.color = "#999";
        meta.style.marginTop = "3px";
        const ts = createdAt ? new Date(createdAt) : new Date();
        meta.innerText = (mine ? "Вы" : "Клиент") + " • " + ts.toLocaleTimeString();

        wrap.appendChild(row);
        wrap.appendChild(meta);

        chatBox.appendChild(wrap);
        chatBox.scrollTop = chatBox.scrollHeight;
    }

    async function loadHistory(chatId) {
        if (!cfg.apiBase || !chatId) return;
        const resp = await fetch(`${cfg.apiBase}/api/ChatMessages/chat/${chatId}`);
        if (!resp.ok) return;
        const data = await resp.json();
        chatBox.innerHTML = "";
        for (const m of data) {
            renderMessage({ senderRole: m.senderRole, body: m.body, createdAt: m.createdAt });
        }
    }

    function liForChat(c, withButton, buttonText, buttonHandler) {
        const li = document.createElement("li");
        li.className = "list-group-item d-flex justify-content-between align-items-center";
        li.dataset.chatId = c.idChat;

        const created = c.startedAt ? new Date(c.startedAt).toLocaleString() : "";
        const left = document.createElement("div");
        left.innerHTML = `
      <div><b>Чат</b> • ${created}</div>
      <div class="text-muted" style="font-size:12px;">
        Клиент ${c.customerUserId} • ${c.status}, приоритет ${c.priority}
      </div>`;
        li.appendChild(left);

        const right = document.createElement("div");
        if (withButton) {
            const btn = document.createElement("button");
            btn.className = "btn btn-sm btn-outline-primary";
            btn.innerText = buttonText;
            btn.addEventListener("click", () => buttonHandler(c));
            right.appendChild(btn);
        } else {
            const btn = document.createElement("button");
            btn.className = "btn btn-sm btn-outline-secondary";
            btn.innerText = "Открыть";
            btn.addEventListener("click", () => openChat(c)); 
            right.appendChild(btn);
        }
        li.appendChild(right);
        return li;
    }

    async function loadQueue() {
        const url = cfg.isAdmin
            ? `${cfg.apiBase}/api/ChatSessions/open`
            : `${cfg.apiBase}/api/ChatSessions/queue`;
        const resp = await fetch(url);
        if (!resp.ok) return;
        const data = await resp.json();
        queueList.innerHTML = "";
        for (const c of data) {
            // админ видит и назначенные, и неназначенные
            const canClaim = !c.assignedAgentId;
            const li = cfg.isAdmin
                ? (canClaim ? liForChat(c, true, "Взять", claimChat) : liForChat(c, false))
                : liForChat(c, true, "Взять", claimChat); // менеджеру в очереди показываем только не назначенные
            queueList.appendChild(li);
        }
    }

    async function loadMyActive() {
        const resp = await fetch(`${cfg.apiBase}/api/ChatSessions/active/${cfg.agentId}`);
        if (!resp.ok) return;
        const data = await resp.json();
        activeList.innerHTML = "";
        for (const c of data) {
            const li = liForChat(c, false);
            activeList.appendChild(li);
        }
    }

    async function loadClosed() {
        if (!closedList) return; 
        const url = cfg.isAdmin
            ? `${cfg.apiBase}/api/ChatSessions/closed`
            : `${cfg.apiBase}/api/ChatSessions/closed-by-agent/${cfg.agentId}`;
        const resp = await fetch(url);
        if (!resp.ok) return;
        const data = await resp.json();
        closedList.innerHTML = "";
        for (const c of data) {
            const li = liForChat(c, false);
            closedList.appendChild(li);
        }
    }

    // Кнопка "Взять"
    async function claimChat(c) {
        try {
            await connection.invoke("ClaimChat", c.idChat, cfg.agentId);
        } catch (e) {
            console.error(e);
            setStatus("Не удалось взять чат");
        }
    }

    async function fetchChat(id) {
        const resp = await fetch(`${cfg.apiBase}/api/ChatSessions/${id}`);
        if (!resp.ok) throw new Error("chat load failed");
        return await resp.json();
    }

    async function openChat(cOrId) {
        const c = typeof cOrId === "number" ? await fetchChat(cOrId) : cOrId;
        const chatId = c.idChat;
        currentChatId = chatId;

        const created = c.startedAt ? new Date(c.startedAt).toLocaleString() : "";
        chatTitle.innerText = `Чат • ${created}`;

        const isClosed = c.status === "closed";
        msgInput.disabled = isClosed;
        sendBtn.disabled = isClosed;
        closeChatBtn.disabled = isClosed; 

        await connection.invoke("JoinChat", chatId);
        await loadHistory(chatId);
    }

    async function send() {
        if (!currentChatId) return;
        if (msgInput.disabled) return;
        const text = msgInput.value.trim();
        if (!text) return;
        msgInput.value = "";
        await connection.invoke("SendFromConsultant", currentChatId, cfg.agentId, text);
    }

    async function closeCurrent() {
        if (!currentChatId) return;
        await connection.invoke("CloseChat", currentChatId, cfg.agentId);
    }

    function onChatClaimed(payload) {
        const { chatId, agentId } = payload;

        [...queueList.children].forEach(li => {
            if (parseInt(li.dataset.chatId, 10) === chatId) li.remove();
        });

        if (agentId === cfg.agentId) {
            openChat(chatId);
            loadMyActive();
        } else {
            loadMyActive();
        }
    }

    function onChatClosed(payload) {
        const { chatId } = payload;
        if (currentChatId === chatId) {
            chatTitle.innerText = "Чат закрыт";
            msgInput.disabled = true;
            sendBtn.disabled = true;
            closeChatBtn.disabled = true;
        }
        loadQueue();
        loadMyActive();
        loadClosed();
    }

    async function init() {
        if (typeof signalR === "undefined") {
            setStatus("Не найдена библиотека SignalR. Подключите signalr.min.js.");
            return;
        }

        connection = new signalR.HubConnectionBuilder()
            .withUrl(cfg.hubUrl)
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveMessage", (msg) => {
            if (msg.chatId && msg.chatId === currentChatId) {
                renderMessage(msg);
            }
        });

        connection.on("ChatClaimed", onChatClaimed);
        connection.on("ChatClosed", onChatClosed);

        connection.onreconnecting(() => setStatus("Пытаемся восстановить соединение..."));
        connection.onreconnected(() => setStatus(""));

        setStatus("Подключаемся…");
        await connection.start();
        setStatus("");

        await loadQueue();
        await loadMyActive();
        await loadClosed();

        refreshQueueBtn?.addEventListener("click", async () => {
            await loadQueue();
            await loadMyActive();
            await loadClosed();
        });

        sendBtn?.addEventListener("click", send);
        msgInput?.addEventListener("keydown", (e) => {
            if (e.key === "Enter") { e.preventDefault(); send(); }
        });
        closeChatBtn?.addEventListener("click", closeCurrent);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
