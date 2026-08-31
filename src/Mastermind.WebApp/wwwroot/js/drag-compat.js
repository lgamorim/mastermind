// Firefox will not begin a drag on a draggable element unless the dragstart
// handler writes something to the DataTransfer. Blazor's C# handlers cannot do
// it: DragEventArgs.DataTransfer is a deserialized copy, so assigning to it
// never reaches the live DOM event. Supply the minimum payload here and let the
// component's own @ondragstart keep owning the state.
//
// This only makes dragging work in Firefox; the tap path is what makes the game
// playable without a drag at all.
document.addEventListener('dragstart', (event) => {
    if (event.target instanceof Element && event.target.closest('.palette-peg')) {
        event.dataTransfer?.setData('text/plain', '');
    }
});
