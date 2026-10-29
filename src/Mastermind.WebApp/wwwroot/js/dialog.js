// Blazor can call a JavaScript function but not a method on an element, so the
// help dialog opens as a modal through this. Closing needs no script: the
// dialog's <form method="dialog"> and the Esc key close it natively.
window.mastermindDialog = {
    showModal: (dialog) => dialog.showModal(),
};
