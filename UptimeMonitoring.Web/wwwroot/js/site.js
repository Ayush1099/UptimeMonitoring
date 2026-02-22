// Sticky footer and alert auto-dismiss
(function () {
  document.addEventListener('DOMContentLoaded', function () {
    // Auto-dismiss temp alerts after 5 seconds
    var alerts = document.querySelectorAll('.temp-alert[data-auto-dismiss]');
    alerts.forEach(function (alert) {
      var ms = parseInt(alert.getAttribute('data-auto-dismiss'), 10) || 5000;
      setTimeout(function () {
        var bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
        if (bsAlert) bsAlert.close();
      }, ms);
    });
  });
})();
