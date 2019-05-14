using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CoreLib.Tools
{
    public static class StaticExtensions
    {
        public static async Task PlayStreamAsync(this MediaElement mediaElement, IRandomAccessStream stream, bool disposeStream = true,Action feedback=null)
        {
            // bool is irrelevant here, just using this to flag task completion.
            TaskCompletionSource<bool> taskCompleted = new TaskCompletionSource<bool>();

            // Note that the MediaElement needs to be in the UI tree for events
            // like MediaEnded to fire.
            RoutedEventHandler endOfPlayHandler = (s, e) =>
            {
                if (disposeStream)
                {
                    stream.Dispose();
                }
                taskCompleted.SetResult(true);
                feedback?.Invoke();
            };
            mediaElement.MediaEnded += endOfPlayHandler;

            mediaElement.SetSource(stream, string.Empty);
            mediaElement.Play();

            await taskCompleted.Task;
            mediaElement.MediaEnded -= endOfPlayHandler;
        }
    }
}
