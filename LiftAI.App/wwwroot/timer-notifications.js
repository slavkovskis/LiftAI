(function () {
    "use strict";

    var audioContext = null;

    function getAudioContext() {
        if (audioContext) {
            return audioContext;
        }

        var Ctor = window.AudioContext || window.webkitAudioContext;
        if (!Ctor) {
            return null;
        }

        audioContext = new Ctor();
        return audioContext;
    }

    async function primeAudio() {
        var context = getAudioContext();
        if (!context) {
            return;
        }

        if (context.state === "suspended") {
            await context.resume();
        }
    }

    async function playDoneTone() {
        var context = getAudioContext();
        if (!context) {
            return;
        }

        if (context.state === "suspended") {
            await context.resume();
        }

        var now = context.currentTime;
        var oscillator = context.createOscillator();
        var gainNode = context.createGain();

        oscillator.type = "sine";
        oscillator.frequency.setValueAtTime(880, now);

        gainNode.gain.setValueAtTime(0.0001, now);
        gainNode.gain.exponentialRampToValueAtTime(0.25, now + 0.02);
        gainNode.gain.exponentialRampToValueAtTime(0.0001, now + 0.45);

        oscillator.connect(gainNode);
        gainNode.connect(context.destination);

        oscillator.start(now);
        oscillator.stop(now + 0.45);
    }

    window.liftAiTimerNotifications = {
        primeAudio: primeAudio,
        playDoneTone: playDoneTone
    };
})();

