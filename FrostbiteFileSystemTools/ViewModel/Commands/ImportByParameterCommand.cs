﻿namespace FrostbiteFileSystemTools.ViewModel.Commands
{
    internal class ImportByParameterCommand : AbstractParameterCommand
    {
        protected override void DoSpecificWork()
        {
            myOneTimeRunViewModel.Import();
        }
    }
}
