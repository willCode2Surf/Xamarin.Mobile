using System;
using MonoTouch.UIKit;
using System.Threading.Tasks;
using System.IO;
using MonoTouch.Foundation;

namespace Xamarin.Media
{
	internal class MediaPickerDelegate
		: UIImagePickerControllerDelegate
	{
		internal MediaPickerDelegate (StoreMediaOptions options)
		{
			this.options = options;
		}
		
		public Task<MediaFile> Task
		{
			get { return tcs.Task; }
		}
		
		public override void FinishedPickingMedia (UIImagePickerController picker, MonoTouch.Foundation.NSDictionary info)
		{
			Func<Stream> streamGetter = null;
			switch ((NSString)info[UIImagePickerController.MediaType])
			{
				case MediaPicker.TypeImage:
					streamGetter = () =>
					{
						var image = (UIImage)info[UIImagePickerController.EditedImage];
						if (image == null)
							image = (UIImage)info[UIImagePickerController.OriginalImage];

						switch (this.options.Location)
						{
							case MediaFileStoreLocation.None:
								return new NSDataStream (image.AsJPEG());
						
							case MediaFileStoreLocation.Local:
								string path = GetOutputPath (MediaPicker.TypeImage, options.Directory ?? String.Empty, options.Name);

								using (FileStream fs = File.OpenWrite (path))
								using (Stream s = new NSDataStream (image.AsJPEG()))
								{
									byte[] buffer = new byte[20480];
									int len;
									while ((len = s.Read (buffer, 0, buffer.Length)) > 0)
										fs.Write (buffer, 0, len);

									s.Flush();
								}
						
								return File.OpenRead (path);
								break;
						
							case MediaFileStoreLocation.CameraRoll:
								throw new NotImplementedException();
						
							default:
								throw new NotSupportedException();
						}
					};
					break;
				
				case MediaPicker.TypeVideo:
					streamGetter = () =>
					{
						throw new NotImplementedException();
					};
					break;
				
				default:
					throw new NotSupportedException();
			}

			this.tcs.TrySetResult (new MediaFile (streamGetter, null));
			picker.DismissModalViewControllerAnimated (animated: true);
		}
		
		public override void Canceled (UIImagePickerController picker)
		{
			this.tcs.TrySetCanceled();
		}
		
		private TaskCompletionSource<MediaFile> tcs = new TaskCompletionSource<MediaFile>();
		private StoreMediaOptions options;
		private bool isPhoto;
		
		private static string GetUniquePath (string type, string path, string name)
		{
			bool isPhoto = (type == MediaPicker.TypeImage);
			string ext = Path.GetExtension (name);
			if (ext == String.Empty)
				ext = ((isPhoto) ? ".jpg" : "mp4");
			
			name = Path.GetFileNameWithoutExtension (name);
			
			string nname = name + ext;
			int i = 1;
			while (File.Exists (Path.Combine (path, nname)))
				nname = name + "_" + (i++) + ext;
			
			return Path.Combine (path, nname);
		}

		private static string GetOutputPath (string type, string path, string name)
		{
			path = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), path);
			if (String.IsNullOrWhiteSpace (name))
			{
				string timestamp = DateTime.Now.ToString ("yyyMMdd_HHmmss");
				if (type == MediaPicker.TypeImage)
					name = "IMG_" + timestamp + ".jpg";
				else
					name = "VID_" + timestamp + ".mp4";
			}
			
			return Path.Combine (path, GetUniquePath (type, path, name));
		}
	}
}