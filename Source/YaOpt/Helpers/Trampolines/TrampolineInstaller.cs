using System;
using System.Reflection;

namespace YaOpt.Helpers.Trampolines
{
	public abstract class TrampolineInstaller
	{
		public bool Installed { protected set; get; }

		public bool Available { protected set; get; }

		protected MethodInfo SourceMethod { set; get; }

		protected MethodInfo TargetMethod { set; get; }

		protected byte[] PrefixCode { set; get; }

		protected Trampoline Trampoline { set; get; }

		public void Init()
		{
			Prepare();
			AfterPrepare();
			CreateCode();
			CreateTrampoline();
			Validate();
			Available = true;
		}

		public void TryInstall()
		{
			try
			{
				if (!Installed && Available && ShouldInstall())
				{
					Install();
					Installed = true;
				}
			}
			catch (Exception)
			{
				Installed = false;
				Available = false;
				throw;
			}
		}

		public void TryUninstall()
		{
			try
			{
				if (Installed)
				{
					Uninstall();
					Installed = false;
				}
			}
			catch (Exception)
			{
				Installed = false;
				Available = false;
				throw;
			}
		}

		protected abstract void Prepare();

		protected virtual void AfterPrepare()
		{
			if (SourceMethod == null)
				throw new MissingMethodException($"Cannot find source method for {GetType().Name}");
			if (TargetMethod == null)
				throw new MissingMethodException($"Cannot find target method for {GetType().Name}");
		}

		protected abstract void CreateCode();

		protected virtual void CreateTrampoline()
		{
			if (PrefixCode == null)
				throw new Exception($"TrampolineInstaller {GetType().Name} didn't create any prefix code.");
			Trampoline = TrampolineFactory.Instance.CreateTrampoline(SourceMethod, TargetMethod, PrefixCode);
		}

		protected abstract bool ShouldInstall();

		protected virtual void Validate()
		{
		}

		protected virtual void Install()
		{
			Trampoline.Install();
		}

		protected virtual void Uninstall()
		{
			Trampoline.Uninstall();
		}
	}
}