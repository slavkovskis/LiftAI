window.chatUi = {
    setBodyScrollLock: function (isLocked) {
        if (isLocked) {
            document.body.classList.add("chat-lock");
        } else {
            document.body.classList.remove("chat-lock");
        }
    },
    scrollToBottomIfNearBottom: function (selector, threshold) {
        var container = document.querySelector(selector);
        if (!container) {
            return;
        }

        var maxGap = typeof threshold === "number" ? threshold : 120;
        var distanceFromBottom = container.scrollHeight - container.scrollTop - container.clientHeight;
        if (distanceFromBottom <= maxGap) {
            container.scrollTop = container.scrollHeight;
        }
    },
    autoGrow: function (element) {
         if (!element || !element.style || typeof element.scrollHeight !== "number") {
            return;
        }

        element.style.height = "auto";
        element.style.height = element.scrollHeight + "px";
    }
};