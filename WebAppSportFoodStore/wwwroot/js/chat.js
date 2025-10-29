(function () {
    if (window.__chatInited) return;
    window.__chatInited = true;

    const $ = (id) => document.getElementById(id);
    const setStatus = (t) => { const s = $("status"); if (s) s.innerText = t || ""; };

    function readConfig() {
        const root = $("chat-root");
        return {
            hubUrl: root?.dataset?.hubUrl || "/hubs/chat",
            apiBase: (root?.dataset?.apiBase || "").replace(/\/$/, ""), 
            customerUserId: parseInt(root?.dataset?.customerId || "0", 10)
        };
    }

    function renderFactory(currentUserId) {
        return function renderMessage({ senderUserId, body, createdAt }) {
            const box = $("chatBox");
            if (!box) return;

            const mine = Number(senderUserId) === currentUserId;
            const wrap = document.createElement("div");
            wrap.style.marginBottom = "8px";

            const bubble = document.createElement("div");
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
            meta.innerText = (mine ? "Вы" : "Консультант") + " • " + ts.toLocaleTimeString();

            wrap.appendChild(row);
            wrap.appendChild(meta);

            box.appendChild(wrap);
            box.scrollTop = box.scrollHeight;
        };
    }

    function showPlaceholder() {
        const box = $("chatBox");
        const title = $("chatTitle");
        const msgInput = $("msgInput");
        const sendBtn = $("sendBtn");
        const resolveBtn = $("resolveBtn");

        if (title) title.innerText = "Чат не выбран";
        if (box) {
            box.innerHTML = `<div class="text-muted" style="padding:16px;">
        Начните новый чат или выберите существующий
      </div>`;
        }
        if (msgInput) msgInput.disabled = true;
        if (sendBtn) sendBtn.disabled = true;
        if (resolveBtn) resolveBtn.disabled = true;
    }

    async function loadHistory(apiBase, chatId, render) {
        if (!apiBase || !chatId) return;
        try {
            const resp = await fetch(`${apiBase}/api/ChatMessages/chat/${chatId}`);
            if (!resp.ok) return;
            const data = await resp.json();
            const box = $("chatBox");
            if (box) box.innerHTML = "";
            for (const m of data) {
                render({ senderUserId: m.senderUserId, body: m.body, createdAt: m.createdAt });
            }
        } catch (e) {
            console.warn("history load failed", e);
        }
    }

    function createdTextFromChat(c) {
        const dtStr = c.startedAt || c.lastMessageAt || c.closedAt;
        if (!dtStr) return "";
        const dt = new Date(dtStr);
        return dt.toLocaleString();
    }

    function liChat(c, onClick) {
        const a = document.createElement("a");
        a.href = "javascript:void(0)";
        a.className = "list-group-item list-group-item-action";
        a.dataset.chatId = c.idChat;

        const subtitle = c.status === "open"
            ? "Открыт"
            : ("Закрыт" + (c.closedAt ? " • " + new Date(c.closedAt).toLocaleString() : ""));
        const createdTxt = createdTextFromChat(c);

        a.innerHTML = `<div><b>Чат</b>${createdTxt ? " • " + createdTxt : ""}</div>
                   <div class="text-muted" style="font-size:12px;">${subtitle}</div>`;
        a.addEventListener("click", () => onClick(c));
        return a;
    }

    async function init() {
        const { hubUrl, apiBase, customerUserId } = readConfig();
        if (!customerUserId || Number.isNaN(customerUserId)) {
            setStatus("Не удалось определить текущего пользователя");
            return;
        }

        const myOpenList = $("myChatsOpen");
        const myClosedList = $("myChatsClosed");
        const newChatBtn = $("newChatBtn");
        const chatTitle = $("chatTitle");
        const msgInput = $("msgInput");
        const sendBtn = $("sendBtn");
        const resolveBtn = $("resolveBtn");

        const render = renderFactory(customerUserId);
        let chatId = null;                
        let currentChatStatus = "open";

        // SignalR
        if (typeof signalR === "undefined") {
            setStatus("Не найдена библиотека SignalR. Подключите signalr.min.js.");
            return;
        }
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveMessage", (msg) => {
            if (!chatId || (msg.chatId && msg.chatId !== chatId)) return;
            render(msg);
        });

        connection.on("ChatClosed", ({ chatId: closedId }) => {
            if (chatId === closedId) {
                currentChatStatus = "closed";
                if (msgInput) msgInput.disabled = true;
                if (sendBtn) sendBtn.disabled = true;
                if (resolveBtn) resolveBtn.disabled = true;

                if (chatTitle) {
                    const base = chatTitle.innerText.replace(/\s*\(закрыт\)$/, "");
                    chatTitle.innerText = `${base} (закрыт)`;
                }
                setStatus("Чат закрыт.");
            }
            refreshMyChats();
        });

        connection.onreconnecting(() => setStatus("Соединение потеряно, пытаемся восстановить..."));
        connection.onreconnected(async () => {
            setStatus("");
            if (chatId) {
                try { await connection.invoke("JoinChat", chatId); } catch (e) { console.error(e); }
            }
        });

        await connection.start();

        async function openChat(c) {
            chatId = c.idChat;
            currentChatStatus = c.status;

            const createdTxt = createdTextFromChat(c);
            if (chatTitle) {
                let base = `Чат${createdTxt ? " • " + createdTxt : ""}`;
                if (currentChatStatus === "closed") base += " (закрыт)";
                chatTitle.innerText = base;
            }

            if (msgInput) msgInput.disabled = currentChatStatus !== "open";
            if (sendBtn) sendBtn.disabled = currentChatStatus !== "open";
            if (resolveBtn) resolveBtn.disabled = currentChatStatus !== "open";

            await connection.invoke("JoinChat", chatId);
            await loadHistory(apiBase, chatId, render);
        }

        async function refreshMyChats() {
            const [openResp, closedResp] = await Promise.all([
                fetch(`${apiBase}/api/ChatSessions/by-customer/${customerUserId}/open`),
                fetch(`${apiBase}/api/ChatSessions/by-customer/${customerUserId}/closed`)
            ]);

            let openCount = 0;

            if (openResp.ok) {
                const open = await openResp.json();
                openCount = Array.isArray(open) ? open.length : 0;
                if (myOpenList) {
                    myOpenList.innerHTML = "";
                    for (const c of open) myOpenList.appendChild(liChat(c, openChat));
                }
            }

            if (closedResp.ok) {
                const closed = await closedResp.json();
                if (myClosedList) {
                    myClosedList.innerHTML = "";
                    for (const c of closed) myClosedList.appendChild(liChat(c, openChat));
                }
            }

            if (newChatBtn) newChatBtn.disabled = openCount > 0;

            const hasAny = (myOpenList?.children.length || 0) + (myClosedList?.children.length || 0) > 0;
            if (!hasAny && chatId === null) {
                showPlaceholder();
            }
        }

        async function createNewChat() {
            if (newChatBtn?.disabled) return;
            const resp = await fetch(`${apiBase}/api/ChatSessions/start`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(customerUserId) 
            });
            if (!resp.ok) { setStatus("Не удалось создать чат"); return; }
            const c = await resp.json();
            await openChat(c);
            await refreshMyChats();
        }

        async function send() {
            if (!chatId || currentChatStatus !== "open") return;
            const text = $("msgInput").value.trim();
            if (!text) return;
            $("msgInput").value = "";
            await connection.invoke("SendFromCustomer", chatId, customerUserId, text);
        }

        async function resolve() {
            if (!chatId || currentChatStatus !== "open") return;
            await connection.invoke("CloseByCustomer", chatId, customerUserId);
        }

        $("newChatBtn")?.addEventListener("click", createNewChat);
        $("sendBtn")?.addEventListener("click", send);
        $("msgInput")?.addEventListener("keydown", (e) => { if (e.key === "Enter") { e.preventDefault(); send(); } });
        $("resolveBtn")?.addEventListener("click", resolve);

        showPlaceholder();
        await refreshMyChats();
    }

    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", init);
    else init();
})();
